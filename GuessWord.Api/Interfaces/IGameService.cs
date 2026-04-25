using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace GuessWord.Api.Interfaces
{
    public interface IGameService
    {
        Task<SingleGameResponseDto> StartSingleGameAsync(int userId);
        Task<SingleGameResponseDto?> GetCurrentSingleGameAsync(int userId);
        Task<SingleGameResponseDto> SubmitGuessAsync(int userId, SubmitGuessRequestDto request);
        Task<SingleGameResponseDto> GiveUpSingleGameAsync(int userId, int gameId);
    }
}
