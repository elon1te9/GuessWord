using GuessWord.Api.Interfaces;
using GuessWord.Shared.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GuessWord.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost("single/start")]
        public async Task<IActionResult> StartSingleGame()
        {
            var userId = GetUserId();
            var result = await _gameService.StartSingleGameAsync(userId);
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
    }
}
