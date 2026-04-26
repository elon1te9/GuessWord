using System.Net;
using System.Net.Http.Json;

namespace GuessWord.Client.Services;

public class ApiRequestService
{
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
        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return default;
            }

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return default;
            }

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TResponse>(string url)
    {
        try
        {
            var response = await _httpClient.PostAsync(url, content: null);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return default;
            }

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    public async Task<bool> PostAsync<TRequest>(string url, TRequest data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PostAsync(string url)
    {
        try
        {
            var response = await _httpClient.PostAsync(url, content: null);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(url, data);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return default;
            }

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string url)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task HandleUnauthorizedAsync()
    {
        await _localStorageService.RemoveItemAsync("token");
        await _localStorageService.RemoveItemAsync("login");
        await _localStorageService.RemoveItemAsync("name");

        _httpClient.DefaultRequestHeaders.Authorization = null;
        _userStateService.ClearUser();
    }
}
