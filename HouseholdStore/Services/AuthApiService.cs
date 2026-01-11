using HouseholdStore.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HouseholdStore.Services;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}
public class AuthApiService 
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthApiService(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
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

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies["jwt"];
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await _http.GetAsync($"{BASE_URL}/user?email={email}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<User>();
        }

        return null;
    }
    public async Task<bool> ConfirmEmail(string email, string code)
    {
        var response = await _http.PostAsJsonAsync(
            $"{BASE_URL}/confirm-email",
            new { Email = email, Code = code });

        return response.IsSuccessStatusCode;
    }
    public async Task<bool> ResendConfirmCode(string email)
    {
        var response = await _http.PostAsJsonAsync(
            "resend-confirm-code",
            new { Email = email });

        return response.IsSuccessStatusCode;
    }

    public async Task ForgotPassword(string email)
    {
        await _http.PostAsJsonAsync($"{BASE_URL}/forgot-password", new { Email = email });
    }

    public async Task<bool> ConfirmResetCode(string email, string code)
    {
        var res = await _http.PostAsJsonAsync($"{BASE_URL}/confirm-reset-code",
            new { Email = email, Code = code });

        return res.IsSuccessStatusCode;
    }

    public async Task ResetPassword(string email, string newPassword)
    {
        await _http.PostAsJsonAsync($"{BASE_URL}/reset-password",
            new { Email = email, NewPassword = newPassword });
    }

}
