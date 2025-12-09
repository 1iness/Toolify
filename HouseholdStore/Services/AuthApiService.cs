using System.Net.Http.Json;
using HouseholdStore.Models;

namespace HouseholdStore.Services;

public class AuthApiService 
{
    private readonly HttpClient _http;
    public AuthApiService(HttpClient http)
    {
        _http = http;
    }
    private const string BASE_URL = "https://localhost:7152/api/auth";
    public async Task<bool> Register(RegisterViewModel model)
    {
        var response = await _http.PostAsJsonAsync($"{BASE_URL}/register", model);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> Login(LoginViewModel model)
    {
        var response = await _http.PostAsJsonAsync($"{BASE_URL}/login", model);
        if (!response.IsSuccessStatusCode)
            return null;

        var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return data?.Token;
    }
}
