using GuessWord.Shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace GuessWord.Api.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> Register(RegisterRequestDto request);
        Task<IActionResult> Login(LoginRequestDto request);
    }
}
