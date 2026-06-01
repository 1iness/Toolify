using HouseholdStore.Helpers;
using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toolify.AuthService.Models;
using Toolify.AuthService.Services;
using Toolify.ProductService;

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

            try
            {
                var contactEmail = (vm.GuestEmail ?? string.Empty).Trim();
                string senderLabel;
                if (User.Identity?.IsAuthenticated == true)
                {
                    User? apiUser = null;
                    if (!string.IsNullOrWhiteSpace(contactEmail))
                        apiUser = await _authApi.GetUserByEmailAsync(contactEmail);

                    var first = apiUser?.FirstName?.Trim() ?? "";
                    var last = apiUser?.LastName?.Trim() ?? "";
                    var fullName = $"{first} {last}".Trim();
                    senderLabel = string.IsNullOrWhiteSpace(fullName)
                        ? (!string.IsNullOrWhiteSpace(contactEmail) ? contactEmail : "Пользователь")
                        : fullName;
                }
                else
                {
                    senderLabel = "Гость";
                }

                await _emailService.SendIncomingUserChatToAdminAsync(
                    conversationId.Value,
                    senderLabel,
                    string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail,
                    ChatSubject.Resolve(vm.Subject, vm.MessageText.Trim()),
                    vm.MessageText.Trim());
            }
            catch
            {
                // уведомление на почту не должно ломать отправку в чат
            }

            return Ok(new { conversationId = conversationId.Value });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminUnreadCount()
        {
            var count = await _chatApi.GetAdminUnreadConversationCountAsync();
            return Json(new { count });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            var conversations = await _chatApi.GetAdminConversationsAsync();
            await FillAdminUserViewBagsAsync();
            ViewBag.AdminPanelKey = "chat";
            ViewBag.AdminSearchTarget = "none";
            var chatTitle = AdminPageTitleHelper.GetForPanel("chat");
            ViewBag.AdminSectionTitle = chatTitle;

            if (AdminPartialHelper.IsPartial(Request))
            {
                Response.Headers["X-Admin-Panel"] = "chat";
                Response.Headers["X-Admin-Search"] = "none";
                if (!string.IsNullOrEmpty(chatTitle))
                    Response.Headers["X-Admin-Page-Title"] = AdminPageTitleHelper.EncodeForHeader(chatTitle);
                return PartialView(conversations);
            }

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
            await FillAdminUserViewBagsAsync();
            ViewBag.AdminPanelKey = "chat";
            ViewBag.AdminSearchTarget = "none";
            var chatTitle = AdminPageTitleHelper.GetForPanel("chat");
            ViewBag.AdminSectionTitle = chatTitle;
            ViewData["Title"] = chatTitle ?? "Чаты с клиентами";

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
                var messages = await _chatApi.GetMessagesAsync(conversationId);
                var lastUserMessage = messages
                    .OrderBy(m => m.CreatedAt)
                    .LastOrDefault(m => string.Equals(m.SenderType, "user", StringComparison.OrdinalIgnoreCase));

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
                        var subjectDisplay = ChatSubject.TitleForAdminList(
                            conversation.Subject,
                            conversation.LastMessagePreview,
                            conversation.Id);

                        await _emailService.SendChatReplyAsync(
                            toEmail,
                            subjectDisplay,
                            messageText.Trim(),
                            conversationId,
                            lastUserMessage?.MessageText);
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
        private async Task FillAdminUserViewBagsAsync()
        {
            Dictionary<int, string> emails = new();
            Dictionary<int, string> display = new();

            try
            {
                var users = await _authApi.GetAllUsersAsync();
                foreach (var g in users.Where(u => u.Id > 0).GroupBy(u => u.Id))
                {
                    var u = g.First();
                    if (!string.IsNullOrWhiteSpace(u.Email))
                        emails[u.Id] = u.Email.Trim();
                    var name = $"{u.FirstName?.Trim() ?? ""} {u.LastName?.Trim() ?? ""}".Trim();
                    display[u.Id] = string.IsNullOrWhiteSpace(name)
                        ? (!string.IsNullOrWhiteSpace(u.Email) ? u.Email.Trim() : $"Пользователь {u.Id}")
                        : name;
                }
            }
            catch
            {
                /* оставить пустые словари */
            }

            ViewBag.UserEmailMap = emails;
            ViewBag.UserDisplayMap = display;
        }
    }
}
