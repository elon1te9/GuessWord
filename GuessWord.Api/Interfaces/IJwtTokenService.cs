using GuessWord.Api.Models;
namespace GuessWord.Api.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
