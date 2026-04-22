using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
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
        public async Task<IActionResult> StartSingleGame(int userId)
        {
            return await _gameService.StartSingleGame(userId);
        }

        [HttpGet("single/current")]
        public async Task<IActionResult> GetCurrentSingleGame(int userId)
        {
            return await _gameService.GetCurrentSingleGame(userId);
        }
    }
}