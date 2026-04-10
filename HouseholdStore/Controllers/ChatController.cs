using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Toolify.AuthService.Services;

namespace HouseholdStore.Controllers
{
    public class ChatController : Controller
    {
        private const string ChatGuestCookieName = "ChatGuestId";
        private readonly ChatApiService _chatApi;
        private readonly AuthApiService _authApi;
        private readonly EmailService _emailService;

        public ChatController(ChatApiService chatApi, AuthApiService authApi, EmailService emailService)
        {
            _chatApi = chatApi;
            _authApi = authApi;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> WidgetData()
        {
            var guestId = EnsureGuestIdCookie();
            var conversation = await _chatApi.GetMyConversationAsync(guestId);
            var messages = conversation == null
                ? new List<ChatMessageVm>()
                : await _chatApi.GetMessagesAsync(conversation.Id);

            return Json(new
            {
                conversation,
                messages,
                isAuthenticated = User.Identity?.IsAuthenticated == true
            });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] UserChatMessageVm vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.MessageText))
                return BadRequest("Пустое сообщение.");

            if (User.Identity?.IsAuthenticated == true && string.IsNullOrWhiteSpace(vm.GuestEmail))
            {
                vm.GuestEmail = User.Identity?.Name;
            }

            var guestId = EnsureGuestIdCookie();
            var conversationId = await _chatApi.SendUserMessageAsync(guestId, vm);
            if (!conversationId.HasValue) return BadRequest("Не удалось отправить сообщение.");

            return Ok(new { conversationId = conversationId.Value });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            var conversations = await _chatApi.GetAdminConversationsAsync();
            ViewBag.UserEmailMap = await GetUserEmailMapAsync();
            return View(conversations);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminConversation(int id)
        {
            var conversations = await _chatApi.GetAdminConversationsAsync();
            var selected = conversations.FirstOrDefault(x => x.Id == id);
            if (selected == null) return NotFound();

            ViewBag.Conversations = conversations;
            ViewBag.SelectedConversation = selected;
            ViewBag.UserEmailMap = await GetUserEmailMapAsync();
            var messages = await _chatApi.GetMessagesAsync(id);
            return View(messages);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AdminReply(int conversationId, string messageText)
        {
            if (conversationId <= 0 || string.IsNullOrWhiteSpace(messageText))
                return RedirectToAction("AdminConversation", new { id = conversationId });

            var sent = await _chatApi.SendAdminReplyAsync(conversationId, messageText.Trim());
            if (!sent) return RedirectToAction("AdminConversation", new { id = conversationId });

            try
            {
                var conversations = await _chatApi.GetAdminConversationsAsync();
                var conversation = conversations.FirstOrDefault(x => x.Id == conversationId);
                if (conversation != null)
                {
                    string? toEmail = conversation.GuestEmail;
                    if (string.IsNullOrWhiteSpace(toEmail) && conversation.UserId.HasValue)
                    {
                        var users = await _authApi.GetAllUsersAsync();
                        toEmail = users.FirstOrDefault(x => x.Id == conversation.UserId.Value)?.Email;
                    }

                    if (!string.IsNullOrWhiteSpace(toEmail))
                    {
                        await _emailService.SendChatReplyAsync(
                            toEmail,
                            conversation.Subject,
                            messageText.Trim(),
                            conversationId);
                    }
                }
            }
            catch
            {
            }

            return RedirectToAction("AdminConversation", new { id = conversationId });
        }

        private string EnsureGuestIdCookie()
        {
            var guestId = Request.Cookies[ChatGuestCookieName];
            if (!string.IsNullOrWhiteSpace(guestId)) return guestId;

            guestId = Request.Cookies["GuestId"];
            if (!string.IsNullOrWhiteSpace(guestId))
            {
                Response.Cookies.Append(ChatGuestCookieName, guestId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(60),
                    HttpOnly = true,
                    IsEssential = true
                });
                return guestId;
            }
            if (!string.IsNullOrWhiteSpace(guestId)) return guestId;

            guestId = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(ChatGuestCookieName, guestId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(60),
                HttpOnly = true,
                IsEssential = true
            });
            return guestId;
        }
        private async Task<Dictionary<int, string>> GetUserEmailMapAsync()
        {
            try
            {
                var users = await _authApi.GetAllUsersAsync();
                return users
                    .Where(u => u.Id > 0 && !string.IsNullOrWhiteSpace(u.Email))
                    .GroupBy(u => u.Id)
                    .ToDictionary(g => g.Key, g => g.First().Email);
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }
    }
}
