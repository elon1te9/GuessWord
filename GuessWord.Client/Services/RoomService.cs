using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;

namespace GuessWord.Client.Services;

public class RoomService
{
    private readonly ApiRequestService _apiRequestService;

    public RoomService(ApiRequestService apiRequestService)
    {
        _apiRequestService = apiRequestService;
    }

    public async Task<RoomResponseDto?> CreateRoomAsync()
    {
        return await _apiRequestService.PostAsync<RoomResponseDto>("api/room/create");
    }

    public async Task<RoomResponseDto?> JoinRoomAsync(string code)
    {
        var request = new JoinRoomRequestDto
        {
            Code = code
        };

        return await _apiRequestService.PostAsync<JoinRoomRequestDto, RoomResponseDto>("api/room/join", request);
    }

    public async Task<RoomResponseDto?> GetRoomAsync(string code)
    {
        return await _apiRequestService.GetAsync<RoomResponseDto>($"api/room/{code}");
    }

    public async Task<bool> LeaveRoomAsync(string code)
    {
        return await _apiRequestService.PostAsync($"api/room/{code}/leave");
    }
}
