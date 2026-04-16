using HouseholdStore.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Toolify.AuthService.Models;

namespace HouseholdStore.Services;
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

    public async Task SendPasswordResetAsync(string email)
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
    public async Task<List<User>> GetAllUsersAsync()
    {
        var rawToken = _httpContextAccessor.HttpContext?.Request.Cookies["jwt"];
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new Exception("Auth API error: missing jwt cookie (jwt). Please re-login as Admin.");

        var token = Uri.UnescapeDataString(rawToken);

        using var req = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/users");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Auth API error: {(int)response.StatusCode} {response.StatusCode}. {body}");
        }

        return await response.Content.ReadFromJsonAsync<List<User>>() ?? new List<User>();
    }

    public async Task ChangeUserRoleAsync(int userId, string role)
    {
        var rawToken = _httpContextAccessor.HttpContext?.Request.Cookies["jwt"];
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new Exception("Auth API error: missing jwt cookie (jwt). Please re-login as Admin.");

        var token = Uri.UnescapeDataString(rawToken);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/users/{userId}/role");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { Role = role });

        var response = await _http.SendAsync(req);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Auth API error: {(int)response.StatusCode} {response.StatusCode}. {body}");
        }
    }

    public async Task SetUserBlockedAsync(int userId, bool isBlocked)
    {
        var rawToken = _httpContextAccessor.HttpContext?.Request.Cookies["jwt"];
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new Exception("Auth API error: missing jwt cookie (jwt). Please re-login as Admin.");

        var token = Uri.UnescapeDataString(rawToken);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/users/{userId}/blocked");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { IsBlocked = isBlocked });

        var response = await _http.SendAsync(req);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Auth API error: {(int)response.StatusCode} {response.StatusCode}. {body}");
        }
    }

    public async Task<bool> UpdateProfileAsync(string email, string firstName, string lastName, string phone)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies["jwt"];
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.PutAsJsonAsync($"{BASE_URL}/update-profile", new
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone
        });

        return response.IsSuccessStatusCode;
    }
}
