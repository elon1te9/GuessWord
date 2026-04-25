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
}
