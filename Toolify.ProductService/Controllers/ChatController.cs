using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ChatRepository _repo;

        public ChatController(ChatRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("message/user")]
        public async Task<IActionResult> SendUserMessage([FromBody] UserChatMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MessageText))
                return BadRequest("Пустое сообщение.");

            if (request.UserId == null && string.IsNullOrWhiteSpace(request.GuestId))
                return BadRequest("Не передан идентификатор пользователя или гостя.");

            var conversationId = await _repo.CreateOrGetConversationAsync(
                request.UserId,
                request.GuestId,
                request.GuestEmail,
                request.Subject);

            await _repo.AddChatMessageAsync(conversationId, "user", request.MessageText.Trim());

            return Ok(new { conversationId });
        }

        [HttpGet("conversation")]
        public async Task<IActionResult> GetUserConversation([FromQuery] int? userId, [FromQuery] string? guestId)
        {
            var conversation = await _repo.GetConversationForUserAsync(userId, guestId);
            if (conversation == null) return Ok(null);
            return Ok(conversation);
        }

        [HttpGet("messages/{conversationId:int}")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var messages = await _repo.GetConversationMessagesAsync(conversationId);
            return Ok(messages);
        }

        [HttpGet("admin/conversations")]
        public async Task<IActionResult> GetAdminConversations()
        {
            var conversations = await _repo.GetAdminConversationsAsync();
            return Ok(conversations);
        }

        [HttpPost("admin/reply")]
        public async Task<IActionResult> Reply([FromBody] AdminReplyRequest request)
        {
            if (request == null || request.ConversationId <= 0 || string.IsNullOrWhiteSpace(request.MessageText))
                return BadRequest("Некорректный ответ.");

            await _repo.AddChatMessageAsync(request.ConversationId, "admin", request.MessageText.Trim());
            return Ok();
        }
    }
}
