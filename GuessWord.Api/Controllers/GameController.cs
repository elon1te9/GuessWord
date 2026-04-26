using GuessWord.Api.Hubs;
using GuessWord.Api.Interfaces;
using GuessWord.Shared.Enums;
using GuessWord.Shared.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace GuessWord.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly IHubContext<GameHub> _hubContext;

        public GameController(IGameService gameService, IHubContext<GameHub> hubContext)
        {
            _gameService = gameService;
            _hubContext = hubContext;
        }

        [HttpPost("single/start")]
        public async Task<IActionResult> StartSingleGame()
        {
            var userId = GetUserId();
            var result = await _gameService.StartSingleGameAsync(userId);
            return Ok(result);
        }

        [HttpPost("multiplayer/start/{roomCode}")]
        public async Task<IActionResult> StartMultiplayerGame(string roomCode)
        {
            var userId = GetUserId();
            var gameId = await _gameService.StartMultiplayerGameAsync(userId, roomCode);

            if (!gameId.HasValue)
                return BadRequest("Не удалось запустить игру.");

            var normalizedRoomCode = NormalizeRoomCode(roomCode);

            await _hubContext.Clients.Group($"room-{normalizedRoomCode}")
                .SendAsync("MultiplayerGameStarted", gameId.Value);

            await NotifyGameUpdatedAsync(gameId.Value);

            return Ok(gameId.Value);
        }

        [HttpGet("/api/games/{gameId:int}")]
        public async Task<IActionResult> GetGameState(int gameId)
        {
            var userId = GetUserId();
            var result = await _gameService.GetGameStateAsync(userId, gameId);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("multiplayer/{gameId:int}")]
        public async Task<IActionResult> GetMultiplayerGame(int gameId)
        {
            var userId = GetUserId();
            var result = await _gameService.GetMultiplayerGameAsync(userId, gameId);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("multiplayer/guess")]
        public async Task<IActionResult> SubmitMultiplayerGuess(SubmitGuessRequestDto request)
        {
            if (request.GameId <= 0 || string.IsNullOrWhiteSpace(request.Word))
                return BadRequest("Некорректный запрос.");

            var userId = GetUserId();
            var result = await _gameService.SubmitMultiplayerGuessAsync(userId, request);

            if (result is null)
                return BadRequest();

            await NotifyGameUpdatedAsync(request.GameId);

            return Ok(result);
        }

        [HttpGet("single/current")]
        public async Task<IActionResult> GetCurrentSingleGame()
        {
            var userId = GetUserId();
            var result = await _gameService.GetCurrentSingleGameAsync(userId);
            return Ok(result);
        }

        [HttpPost("single/guess")]
        public async Task<IActionResult> SubmitGuess(SubmitGuessRequestDto request)
        {
            if (request.GameId <= 0 || string.IsNullOrWhiteSpace(request.Word))
                return BadRequest("Некорректный запрос.");

            var userId = GetUserId();
            var result = await _gameService.SubmitGuessAsync(userId, request);
            return Ok(result);
        }

        [HttpPost("single/{gameId:int}/giveup")]
        public async Task<IActionResult> GiveUpSingleGame(int gameId)
        {
            if (gameId <= 0)
                return BadRequest("Некорректный gameId.");

            var userId = GetUserId();
            var result = await _gameService.GiveUpSingleGameAsync(userId, gameId);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = GetUserId();
            return Ok(await _gameService.GetHistoryAsync(userId));
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var userId))
                throw new Exception("Пользователь не авторизован.");

            return userId;
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            return roomCode.Trim().ToUpperInvariant();
        }

        private Task NotifyGameUpdatedAsync(int gameId)
        {
            return _hubContext.Clients.Group($"game-{gameId}")
                .SendAsync("GameUpdated", gameId);
        }
    }
}
