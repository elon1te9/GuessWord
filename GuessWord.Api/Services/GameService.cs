using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
using GuessWord.Shared.Enums;
using GuessWord.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Services
{
    public class GameService : IGameService
    {
        private readonly AppDbContext _context;
        private readonly IDictionaryService _dictionaryService;

        public GameService(AppDbContext context, IDictionaryService dictionaryService)
        {
            _context = context;
            _dictionaryService = dictionaryService;
        }

        public async Task<IActionResult> StartSingleGame(int userId)
        {
            var activeGamePlayer = await _context.GamePlayers
                .Include(gp => gp.Game)
                .FirstOrDefaultAsync(gp =>
                    gp.UserId == userId &&
                    gp.Game.Mode == GameMode.Single &&
                    gp.Game.Status == GameStatus.InProgress);

            if (activeGamePlayer is not null)
            {
                var currentState = await BuildSingleGameState(activeGamePlayer.GameId, userId);
                return new OkObjectResult(currentState);
            }

            var secretWord = _dictionaryService.GetRandomSecretWord();

            var secretWordEntity = await _context.WordDictionaries
                .FirstOrDefaultAsync(x => x.Word == secretWord);

            if (secretWordEntity is null)
            {
                secretWordEntity = new WordDictionary
                {
                    Word = secretWord
                };

                _context.WordDictionaries.Add(secretWordEntity);
                await _context.SaveChangesAsync();
            }

            var game = new Game
            {
                Mode = GameMode.Single,
                Status = GameStatus.InProgress,
                SecretWordId = secretWordEntity.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var gamePlayer = new GamePlayer
            {
                GameId = game.Id,
                UserId = userId,
                AttemptsCount = 0,
                Result = GamePlayerResult.Playing
            };

            _context.GamePlayers.Add(gamePlayer);
            await _context.SaveChangesAsync();

            var response = await BuildSingleGameState(game.Id, userId);
            return new OkObjectResult(response);
        }

        public async Task<IActionResult> GetCurrentSingleGame(int userId)
        {
            var activeGamePlayer = await _context.GamePlayers
                .Include(gp => gp.Game)
                .FirstOrDefaultAsync(gp =>
                    gp.UserId == userId &&
                    gp.Game.Mode == GameMode.Single &&
                    gp.Game.Status == GameStatus.InProgress);

            if (activeGamePlayer is null)
                return new OkObjectResult(null);

            var response = await BuildSingleGameState(activeGamePlayer.GameId, userId);
            return new OkObjectResult(response);
        }

        private async Task<SingleGameStateResponseDto> BuildSingleGameState(int gameId, int userId)
        {
            var game = await _context.Games
                .Include(g => g.Attempts.Where(a => a.UserId == userId))
                .Include(g => g.Players.Where(p => p.UserId == userId))
                .FirstAsync(g => g.Id == gameId);

            var player = game.Players.First();

            return new SingleGameStateResponseDto
            {
                GameId = game.Id,
                Status = game.Status.ToString(),
                AttemptsCount = player.AttemptsCount,
                CreatedAt = game.CreatedAt,
                Attempts = game.Attempts
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new GameAttemptItemDto
                    {
                        Word = a.Word,
                        Rank = a.Rank,
                        IsValid = a.IsValid,
                        CreatedAt = a.CreatedAt
                    })
                    .ToList()
            };
        }
    }
}