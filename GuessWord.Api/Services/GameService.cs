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
            var currentGame = await GetCurrentGameEntityAsync(userId);
            if (currentGame is not null)
                return await BuildGameResponseAsync(currentGame.Id, userId);

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

            if (word is null)
            {
                game.Attempts.Add(new GameAttempt
                {
                    GameId = game.Id,
                    UserId = userId,
                    Word = normalizedWord,
                    Rank = null,
                    IsValid = false
                });

                await _context.SaveChangesAsync();
                return await BuildGameResponseAsync(game.Id, userId);
            }

            var isRepeated = game.Attempts.Any(a => a.UserId == userId && a.Word == normalizedWord && a.IsValid);

            var rank = await _rankService.GetRankAsync(game.SecretWordId, word.Id);

            game.Attempts.Add(new GameAttempt
            {
                GameId = game.Id,
                UserId = userId,
                Word = normalizedWord,
                Rank = rank,
                IsValid = true
            });

            if (!isRepeated)
            {
                player.AttemptsCount++;
            }

            if (word.Id == game.SecretWordId)
            {
                game.Status = GameStatus.Finished;
                game.WinnerUserId = userId;
                game.FinishedAt = DateTime.UtcNow;
                player.Result = GamePlayerResult.Won;
            }

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

        private async Task<SingleGameResponseDto> BuildGameResponseAsync(int gameId, int userId)
        {
            var game = await _context.Games
                .AsNoTracking()
                .Include(g => g.SecretWord)
                .Include(g => g.Attempts.Where(a => a.UserId == userId))
                .Include(g => g.Players.Where(p => p.UserId == userId))
                .FirstAsync(g => g.Id == gameId);

            var player = game.Players.First();
            var repeatedWords = game.Attempts
                .Where(a => a.IsValid)
                .GroupBy(a => a.Word)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            return new SingleGameResponseDto
            {
                GameId = game.Id,
                AttemptsCount = player.AttemptsCount,
                Status = game.Status,
                PlayerResult = player.Result,
                SecretWord = game.Status == GameStatus.Finished ? game.SecretWord.Text : null,
                Attempts = game.Attempts
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new GameAttemptResponseDto
                    {
                        Word = a.Word,
                        Rank = a.Rank,
                        IsValid = a.IsValid,
                        IsRepeated = a.IsValid && repeatedWords.Contains(a.Word),
                        CreatedAt = a.CreatedAt
                    })
                    .ToList()
            };
        }
    }
}
