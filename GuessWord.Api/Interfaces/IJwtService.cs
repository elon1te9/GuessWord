using GuessWord.Api.Models;

namespace GuessWord.Api.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
