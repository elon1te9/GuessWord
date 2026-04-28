using System.Net;
using System.Net.Http.Json;

namespace GuessWord.Client.Services;

public class ApiRequestService
{
    private static readonly string[] UserStorageKeys = ["userId", "token", "login", "name"];

    private readonly HttpClient _httpClient;
    private readonly LocalStorageService _localStorageService;
    private readonly UserStateService _userStateService;

    public ApiRequestService(
        HttpClient httpClient,
        LocalStorageService localStorageService,
        UserStateService userStateService)
    {
        _httpClient = httpClient;
        _localStorageService = localStorageService;
        _userStateService = userStateService;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        return await SendAsync<TResponse>(() => _httpClient.GetAsync(url));
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        return await SendAsync<TResponse>(() => _httpClient.PostAsJsonAsync(url, data));
    }

    public async Task<TResponse?> PostAsync<TResponse>(string url)
    {
        return await SendAsync<TResponse>(() => _httpClient.PostAsync(url, content: null));
    }

    public async Task<bool> PostAsync<TRequest>(string url, TRequest data)
    {
        return await SendAsync(() => _httpClient.PostAsJsonAsync(url, data));
    }

    public async Task<bool> PostAsync(string url)
    {
        return await SendAsync(() => _httpClient.PostAsync(url, content: null));
    }

    private async Task HandleUnauthorizedAsync()
    {
        await ClearSavedUserAsync();
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _userStateService.ClearUser();
    }

    private async Task<TResponse?> SendAsync<TResponse>(Func<Task<HttpResponseMessage>> sendRequest)
    {
        try
        {
            var response = await sendRequest();

            if (!await EnsureAuthorizedAsync(response))
                return default;

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    private async Task<bool> SendAsync(Func<Task<HttpResponseMessage>> sendRequest)
    {
        try
        {
            var response = await sendRequest();

            if (!await EnsureAuthorizedAsync(response))
                return false;

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> EnsureAuthorizedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return true;

        await HandleUnauthorizedAsync();
        return false;
    }

    private async Task ClearSavedUserAsync()
    {
        foreach (var key in UserStorageKeys)
        {
            await _localStorageService.RemoveItemAsync(key);
        }
    }
}
