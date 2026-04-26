using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;

namespace GuessWord.Api.Interfaces
{
    public interface IGameService
    {
        Task<SingleGameResponseDto> StartSingleGameAsync(int userId);
        Task<int?> StartMultiplayerGameAsync(int userId, string roomCode);
        Task<GameStateDto?> GetGameStateAsync(int userId, int gameId);
        Task<MultiplayerGameResponseDto?> GetMultiplayerGameAsync(int userId, int gameId);
        Task<SingleGameResponseDto?> GetCurrentSingleGameAsync(int userId);
        Task<MultiplayerGameResponseDto?> SubmitMultiplayerGuessAsync(int userId, SubmitGuessRequestDto request);
        Task<SingleGameResponseDto> SubmitGuessAsync(int userId, SubmitGuessRequestDto request);
        Task<SingleGameResponseDto> GiveUpSingleGameAsync(int userId, int gameId);
        Task<List<GameHistoryItemResponseDto>> GetHistoryAsync(int userId);
    }
}
