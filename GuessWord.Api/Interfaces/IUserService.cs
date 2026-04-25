using GuessWord.Api.Services;
using GuessWord.Shared.Requests;

namespace GuessWord.Api.Interfaces
{
    public interface IUserService
    {
        Task<AuthOperationResult> Register(RegisterRequestDto request);
        Task<AuthOperationResult> Login(LoginRequestDto request);
    }
}
