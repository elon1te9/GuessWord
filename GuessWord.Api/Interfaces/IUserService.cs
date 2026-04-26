using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;

namespace GuessWord.Api.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto?> Register(RegisterRequestDto request);
        Task<AuthResponseDto?> Login(LoginRequestDto request);
        Task<UserProfileResponseDto?> GetProfileAsync(int userId);
        Task<LeaderboardResponseDto> GetLeaderboardAsync();
    }
}
