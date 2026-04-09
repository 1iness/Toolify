namespace Toolify.ProductService.Models
{
    public class ChatConversation
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? GuestId { get; set; }
        public string? GuestEmail { get; set; }
        public string Subject { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
        public bool HasUnreadForAdmin { get; set; }
        public bool HasUnreadForUser { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserChatMessageRequest
    {
        public int? UserId { get; set; }
        public string? GuestId { get; set; }
        public string? GuestEmail { get; set; }
        public string? Subject { get; set; }
        public string MessageText { get; set; } = string.Empty;
    }

    public class AdminReplyRequest
    {
        public int ConversationId { get; set; }
        public string MessageText { get; set; } = string.Empty;
    }
}
