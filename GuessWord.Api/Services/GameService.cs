using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
using GuessWord.Shared.Enums;
using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Services
{
    public class GameService : IGameService
    {
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
                Result = GamePlayerResult.Playing
            };

            _context.Games.Add(game);
            _context.GamePlayers.Add(gamePlayer);
            await _context.SaveChangesAsync();
            await _rankService.PrepareRankingAsync(secretWord.Id);
            await transaction.CommitAsync();

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
            await using var transaction = await _context.Database.BeginTransactionAsync();

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

            if (word is null)
            {
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

            var existingValidAttempt = await _context.GameAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.GameId == game.Id &&
                    a.UserId == userId &&
                    a.Word == normalizedWord &&
                    a.IsValid);

            if (existingValidAttempt is not null)
                return await BuildGameResponseAsync(game.Id, userId, existingValidAttempt.Id, true);

            var rank = await _rankService.GetRankAsync(game.SecretWordId, word.Id);

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
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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

            await _context.SaveChangesAsync();

            return await BuildGameResponseAsync(game.Id, userId);
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
    }
}
