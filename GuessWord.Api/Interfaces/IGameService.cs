using GuessWord.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace GuessWord.Api.Interfaces
{
    public interface IGameService
    {
        Task<IActionResult> StartSingleGame(int userId);
        Task<IActionResult> GetCurrentSingleGame(int userId);
    }
}
