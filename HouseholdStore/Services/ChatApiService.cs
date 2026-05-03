using HouseholdStore.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace HouseholdStore.Services
{
    public class ChatApiService
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string BaseUrl = "https://localhost:7188/api/chat";

        public ChatApiService(HttpClient http, IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;
            var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("id");
            return int.TryParse(idStr, out var id) ? id : null;
        }

        public async Task<ChatConversationVm?> GetMyConversationAsync(string? guestId)
        {
            var userId = GetCurrentUserId();
            var url = $"{BaseUrl}/conversation?userId={(userId?.ToString() ?? string.Empty)}&guestId={Uri.EscapeDataString(guestId ?? string.Empty)}";
            var response = await _http.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ChatConversationVm?>();
        }

        public async Task<List<ChatMessageVm>> GetMessagesAsync(int conversationId)
        {
            var response = await _http.GetAsync($"{BaseUrl}/messages/{conversationId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return new List<ChatMessageVm>();
            if (!response.IsSuccessStatusCode)
                return new List<ChatMessageVm>();
            return await response.Content.ReadFromJsonAsync<List<ChatMessageVm>>() ?? new List<ChatMessageVm>();
        }

        public async Task<int?> SendUserMessageAsync(string? guestId, UserChatMessageVm vm)
        {
            var userId = GetCurrentUserId();
            var body = new
            {
                UserId = userId,
                GuestId = guestId,
                GuestEmail = vm.GuestEmail,
                Subject = vm.Subject,
                MessageText = vm.MessageText
            };

            var response = await _http.PostAsJsonAsync($"{BaseUrl}/message/user", body);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
            if (data != null && data.TryGetValue("conversationId", out var conversationId))
                return conversationId;

            return null;
        }

        public async Task<List<ChatConversationVm>> GetAdminConversationsAsync()
        {
            return await _http.GetFromJsonAsync<List<ChatConversationVm>>($"{BaseUrl}/admin/conversations")
                   ?? new List<ChatConversationVm>();
        }

        public async Task<int> GetAdminUnreadConversationCountAsync()
        {
            var list = await GetAdminConversationsAsync();
            return list.Count(c => c.HasUnreadForAdmin);
        }

        public async Task<bool> SendAdminReplyAsync(int conversationId, string text)
        {
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/admin/reply", new
            {
                ConversationId = conversationId,
                MessageText = text
            });

            return response.IsSuccessStatusCode;
        }
    }
}
