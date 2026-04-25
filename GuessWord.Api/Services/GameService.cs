using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
using GuessWord.Shared.Enums;
using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GuessWord.Api.Services
{
    public class GameService : IGameService
    {
        private const string ActiveSingleGameIndexName = "IX_GamePlayers_UserId_IsActiveSingleGame";

        private readonly AppDbContext _context;
        private readonly IRankService _rankService;

        public GameService(AppDbContext context, IRankService rankService)
        {
            _context = context;
            _rankService = rankService;
        }

        public async Task<SingleGameResponseDto> StartSingleGameAsync(int userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var currentGamePlayer = await _context.GamePlayers
                .Include(gp => gp.Game)
                .FirstOrDefaultAsync(gp =>
                    gp.UserId == userId &&
                    gp.Game.Mode == GameMode.Single &&
                    gp.Game.Status == GameStatus.InProgress);

            if (currentGamePlayer is not null)
                return await BuildGameResponseAsync(currentGamePlayer.GameId, userId);

            var secretWordsQuery = _context.Words
                .AsNoTracking()
                .Where(w => w.CanBeSecret && w.Embedding != null);

            var secretWordsCount = await secretWordsQuery.CountAsync();
            if (secretWordsCount == 0)
                throw new Exception("Секретные слова не найдены.");

            var randomIndex = Random.Shared.Next(secretWordsCount);

            var secretWord = await secretWordsQuery
                .OrderBy(w => w.Id)
                .Skip(randomIndex)
                .FirstAsync();

            var game = new Game
            {
                Mode = GameMode.Single,
                Status = GameStatus.InProgress,
                SecretWordId = secretWord.Id
            };

            var gamePlayer = new GamePlayer
            {
                Game = game,
                UserId = userId,
                AttemptsCount = 0,
                Result = GamePlayerResult.Playing,
                IsActiveSingleGame = true
            };

            _context.Games.Add(game);
            _context.GamePlayers.Add(gamePlayer);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsActiveSingleGameUniqueViolation(ex))
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                var activeGame = await GetCurrentGameEntityAsync(userId);
                if (activeGame is not null)
                    return await BuildGameResponseAsync(activeGame.Id, userId);

                throw;
            }

            await transaction.CommitAsync();
            await _rankService.PrepareRankingAsync(secretWord.Id);

            return await BuildGameResponseAsync(game.Id, userId);
        }

        public async Task<SingleGameResponseDto?> GetCurrentSingleGameAsync(int userId)
        {
            var currentGame = await GetCurrentGameEntityAsync(userId);
            if (currentGame is null)
                return null;

            return await BuildGameResponseAsync(currentGame.Id, userId);
        }

        public async Task<SingleGameResponseDto> SubmitGuessAsync(int userId, SubmitGuessRequestDto request)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                .Include(g => g.Attempts)
                .Include(g => g.SecretWord)
                .FirstOrDefaultAsync(g => g.Id == request.GameId);

            if (game is null)
                throw new Exception("Игра не найдена.");

            if (game.Mode != GameMode.Single)
                throw new Exception("Неверный режим игры.");

            if (game.Status == GameStatus.Finished)
                throw new Exception("Игра уже завершена.");

            var player = game.Players.FirstOrDefault(p => p.UserId == userId);
            if (player is null)
                throw new Exception("Игрок не найден в этой игре.");

            var normalizedWord = request.Word.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(normalizedWord))
                throw new Exception("Слово пустое.");

            var word = await _context.Words
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Text == normalizedWord);

            var existingValidAttempt = await FindExistingValidAttemptAsync(game.Id, userId, normalizedWord);

            if (word is not null && existingValidAttempt is not null)
                return await BuildGameResponseAsync(game.Id, userId, existingValidAttempt.Id, true);

            if (word is null)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var invalidAttempt = new GameAttempt
                {
                    GameId = game.Id,
                    UserId = userId,
                    Word = normalizedWord,
                    Rank = null,
                    IsValid = false
                };

                game.Attempts.Add(invalidAttempt);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return await BuildGameResponseAsync(game.Id, userId, invalidAttempt.Id, false);
            }

            var rank = await _rankService.GetRankAsync(game.SecretWordId, word.Id);
            await using var validAttemptTransaction = await _context.Database.BeginTransactionAsync();

            existingValidAttempt = await FindExistingValidAttemptAsync(game.Id, userId, normalizedWord);
            if (existingValidAttempt is not null)
            {
                await validAttemptTransaction.RollbackAsync();
                return await BuildGameResponseAsync(game.Id, userId, existingValidAttempt.Id, true);
            }

            var newAttempt = new GameAttempt
            {
                GameId = game.Id,
                UserId = userId,
                Word = normalizedWord,
                Rank = rank,
                IsValid = true
            };

            game.Attempts.Add(newAttempt);
            player.AttemptsCount++;

            if (word.Id == game.SecretWordId)
            {
                game.Status = GameStatus.Finished;
                game.WinnerUserId = userId;
                game.FinishedAt = DateTime.UtcNow;
                player.Result = GamePlayerResult.Won;
                player.IsActiveSingleGame = false;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                await validAttemptTransaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                existingValidAttempt = await FindExistingValidAttemptAsync(game.Id, userId, normalizedWord);
                if (existingValidAttempt is not null)
                    return await BuildGameResponseAsync(game.Id, userId, existingValidAttempt.Id, true);

                throw;
            }

            await validAttemptTransaction.CommitAsync();

            return await BuildGameResponseAsync(game.Id, userId, newAttempt.Id, false);
        }

        public async Task<SingleGameResponseDto> GiveUpSingleGameAsync(int userId, int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                .Include(g => g.Attempts)
                .Include(g => g.SecretWord)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game is null)
                throw new Exception("Игра не найдена.");

            if (game.Mode != GameMode.Single)
                throw new Exception("Неверный режим игры.");

            if (game.Status == GameStatus.Finished)
                throw new Exception("Игра уже завершена.");

            var player = game.Players.FirstOrDefault(p => p.UserId == userId);
            if (player is null)
                throw new Exception("Игрок не найден в этой игре.");

            game.Status = GameStatus.Finished;
            game.FinishedAt = DateTime.UtcNow;
            game.WinnerUserId = null;

            player.Result = GamePlayerResult.GaveUp;
            player.IsActiveSingleGame = false;

            await _context.SaveChangesAsync();

            return await BuildGameResponseAsync(game.Id, userId);
        }

        public async Task<List<GameHistoryItemResponseDto>> GetHistoryAsync(int userId)
        {
            return await _context.GamePlayers
                .AsNoTracking()
                .Where(gp => gp.UserId == userId && gp.Game.Status == GameStatus.Finished)
                .Include(gp => gp.Game)
                    .ThenInclude(g => g.SecretWord)
                .OrderByDescending(gp => gp.Game.FinishedAt ?? gp.Game.CreatedAt)
                .Select(gp => new GameHistoryItemResponseDto
                {
                    Date = gp.Game.FinishedAt ?? gp.Game.CreatedAt,
                    GameType = gp.Game.Mode == GameMode.Single ? "Сингл" : "Мультиплеер",
                    Result = gp.Result == GamePlayerResult.Won
                        ? "Победа"
                        : gp.Result == GamePlayerResult.GaveUp || gp.Result == GamePlayerResult.Lost
                            ? "Поражение"
                            : "Завершена",
                    AttemptsCount = gp.AttemptsCount,
                    SecretWord = gp.Game.SecretWord.Text,
                    OpponentName = "-"
                })
                .ToListAsync();
        }

        private async Task<Game?> GetCurrentGameEntityAsync(int userId)
        {
            var gamePlayer = await _context.GamePlayers
                .Include(gp => gp.Game)
                .FirstOrDefaultAsync(gp =>
                    gp.UserId == userId &&
                    gp.Game.Mode == GameMode.Single &&
                    gp.Game.Status == GameStatus.InProgress);

            return gamePlayer?.Game;
        }

        private async Task<GameAttempt?> FindExistingValidAttemptAsync(int gameId, int userId, string normalizedWord)
        {
            return await _context.GameAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.GameId == gameId &&
                    a.UserId == userId &&
                    a.Word == normalizedWord &&
                    a.IsValid);
        }

        private async Task<SingleGameResponseDto> BuildGameResponseAsync(
            int gameId,
            int userId,
            int? lastAttemptId = null,
            bool lastAttemptWasRepeated = false)
        {
            var game = await _context.Games
                .AsNoTracking()
                .Include(g => g.SecretWord)
                .Include(g => g.Attempts.Where(a => a.UserId == userId))
                .Include(g => g.Players.Where(p => p.UserId == userId))
                .FirstAsync(g => g.Id == gameId);

            var player = game.Players.First();
            var attempts = game.Attempts
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new GameAttemptResponseDto
                {
                    Word = a.Word,
                    Rank = a.Rank,
                    IsValid = a.IsValid,
                    IsRepeated = false,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            GameAttemptResponseDto? lastAttempt = null;

            if (lastAttemptId.HasValue)
            {
                var sourceAttempt = game.Attempts.FirstOrDefault(a => a.Id == lastAttemptId.Value);

                if (sourceAttempt is not null)
                {
                    lastAttempt = new GameAttemptResponseDto
                    {
                        Word = sourceAttempt.Word,
                        Rank = sourceAttempt.Rank,
                        IsValid = sourceAttempt.IsValid,
                        IsRepeated = lastAttemptWasRepeated,
                        CreatedAt = sourceAttempt.CreatedAt
                    };
                }
            }

            return new SingleGameResponseDto
            {
                GameId = game.Id,
                AttemptsCount = player.AttemptsCount,
                Status = game.Status,
                PlayerResult = player.Result,
                SecretWord = game.Status == GameStatus.Finished ? game.SecretWord.Text : null,
                LastAttempt = lastAttempt,
                LastAttemptWasRepeated = lastAttemptWasRepeated,
                Attempts = attempts
            };
        }

        private static bool IsActiveSingleGameUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException &&
                   postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                   postgresException.ConstraintName == ActiveSingleGameIndexName;
        }
    }
}
