using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;
using System.Net.Http.Headers;

namespace GuessWord.Client.Services
{
    public class AuthService
    {
        private static readonly string[] UserStorageKeys = ["userId", "token", "login", "name"];

        private readonly ApiRequestService _apiRequestService;
        private readonly LocalStorageService _localStorageService;
        private readonly UserStateService _userStateService;
        private readonly HttpClient _httpClient;

        public AuthService(
            ApiRequestService apiRequestService,
            LocalStorageService localStorageService,
            UserStateService userStateService,
            HttpClient httpClient)
        {
            _apiRequestService = apiRequestService;
            _localStorageService = localStorageService;
            _userStateService = userStateService;
            _httpClient = httpClient;
        }

        public async Task<AuthResponseDto?> Register(RegisterRequestDto request)
        {
            var result = await _apiRequestService.PostAsync<RegisterRequestDto, AuthResponseDto>("api/user/register", request);

            if (result is not null)
            {
                await SaveUserData(result);
            }

            return result;
        }

        public async Task<AuthResponseDto?> Login(LoginRequestDto request)
        {
            var result = await _apiRequestService.PostAsync<LoginRequestDto, AuthResponseDto>("api/user/login", request);

            if (result is not null)
            {
                await SaveUserData(result);
            }

            return result;
        }

        public async Task RestoreUser()
        {
            var userIdValue = await _localStorageService.GetItemAsync("userId");
            var token = await _localStorageService.GetItemAsync("token");
            var login = await _localStorageService.GetItemAsync("login");
            var name = await _localStorageService.GetItemAsync("name");

            if (int.TryParse(userIdValue, out var userId) &&
                !string.IsNullOrWhiteSpace(token) &&
                !string.IsNullOrWhiteSpace(login) &&
                !string.IsNullOrWhiteSpace(name))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                _userStateService.SetUser(userId, token, login, name);
            }

            _userStateService.MarkInitialized();
        }

        public async Task Logout()
        {
            await ClearSavedUserDataAsync();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _userStateService.ClearUser();
        }

        private async Task SaveUserData(AuthResponseDto authData)
        {
            await _localStorageService.SetItemAsync("userId", authData.UserId.ToString());
            await _localStorageService.SetItemAsync("token", authData.Token);
            await _localStorageService.SetItemAsync("login", authData.Login);
            await _localStorageService.SetItemAsync("name", authData.Name);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authData.Token);

            _userStateService.SetUser(authData.UserId, authData.Token, authData.Login, authData.Name);
        }

        private async Task ClearSavedUserDataAsync()
        {
            foreach (var key in UserStorageKeys)
            {
                await _localStorageService.RemoveItemAsync(key);
            }
        }
    }
}
