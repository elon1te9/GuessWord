using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;

namespace GuessWord.Client.Services;

public class GameService
{
    private readonly ApiRequestService _apiRequestService;

    public GameService(ApiRequestService apiRequestService)
    {
        _apiRequestService = apiRequestService;
    }

    public async Task<SingleGameResponseDto?> StartSingleGameAsync()
    {
        return await _apiRequestService.PostAsync<SingleGameResponseDto>("api/game/single/start");
    }

    public async Task<SingleGameResponseDto?> GetCurrentSingleGameAsync()
    {
        return await _apiRequestService.GetAsync<SingleGameResponseDto>("api/game/single/current");
    }

    public async Task<ActiveGameDto?> GetActiveGameAsync()
    {
        return await _apiRequestService.GetAsync<ActiveGameDto>("api/game/active");
    }

    public async Task<SingleGameResponseDto?> SubmitGuessAsync(int gameId, string word)
    {
        var request = new SubmitGuessRequestDto
        {
            GameId = gameId,
            Word = word
        };

        return await _apiRequestService.PostAsync<SubmitGuessRequestDto, SingleGameResponseDto>("api/game/single/guess", request);
    }

    public async Task<SingleGameResponseDto?> GiveUpSingleGameAsync(int gameId)
    {
        return await _apiRequestService.PostAsync<SingleGameResponseDto>($"api/game/single/{gameId}/giveup");
    }

    public async Task<List<GameHistoryItemResponseDto>?> GetHistoryAsync()
    {
        return await _apiRequestService.GetAsync<List<GameHistoryItemResponseDto>>("api/game/history");
    }

    public async Task<int?> StartMultiplayerGameAsync(string roomCode)
    {
        return await _apiRequestService.PostAsync<int>($"api/game/multiplayer/start/{roomCode}");
    }

    public async Task<GameStateDto?> GetGameStateAsync(int gameId)
    {
        return await _apiRequestService.GetAsync<GameStateDto>($"api/games/{gameId}");
    }

    public async Task<GameStateDto?> GiveUpMultiplayerGameAsync(int gameId)
    {
        return await _apiRequestService.PostAsync<GameStateDto>($"api/games/{gameId}/multiplayer/give-up");
    }

    public async Task<MultiplayerGameResponseDto?> SubmitMultiplayerGuessAsync(int gameId, string word)
    {
        var request = new SubmitGuessRequestDto
        {
            GameId = gameId,
            Word = word
        };

        return await _apiRequestService.PostAsync<SubmitGuessRequestDto, MultiplayerGameResponseDto>("api/game/multiplayer/guess", request);
    }
}
