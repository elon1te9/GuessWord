using GuessWord.Api.Interfaces;
using GuessWord.Shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace GuessWord.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var result = await _userService.Register(request);
            return result.Success ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result = await _userService.Login(request);
            return result.Success ? Ok(result.Data) : BadRequest(result.Error);
        }
    }
}
