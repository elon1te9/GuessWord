using GuessWord.Shared.Responses;

namespace GuessWord.Client.Services;

public class UserService
{
    private readonly ApiRequestService _apiRequestService;

    public UserService(ApiRequestService apiRequestService)
    {
        _apiRequestService = apiRequestService;
    }

    public async Task<UserProfileResponseDto?> GetProfileAsync()
    {
        return await _apiRequestService.GetAsync<UserProfileResponseDto>("api/user/profile");
    }
}
