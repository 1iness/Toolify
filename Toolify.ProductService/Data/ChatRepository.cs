using System.Data;
using System.Data.SqlClient;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Data
{
    public class ChatRepository
    {
        private readonly string _chatConnectionString;

        public ChatRepository(IConfiguration configuration)
        {
            _chatConnectionString = configuration.GetConnectionString("AuthDb")
                ?? throw new InvalidOperationException("Connection string 'AuthDb' is missing.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_chatConnectionString);

        public async Task<int> CreateOrGetConversationAsync(int? userId, string? guestId, string? guestEmail, string? subject)
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand("sp_Chat_CreateOrGetConversation", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestEmail", (object?)guestEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@Subject", (object?)subject ?? "Вопрос по сайту");

            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task AddChatMessageAsync(int conversationId, string senderType, string messageText)
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand("sp_Chat_AddMessage", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ConversationId", conversationId);
            command.Parameters.AddWithValue("@SenderType", senderType);
            command.Parameters.AddWithValue("@MessageText", messageText);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<ChatConversation?> GetConversationForUserAsync(int? userId, string? guestId)
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand("sp_Chat_GetConversationForUser", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new ChatConversation
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                GuestId = reader.IsDBNull(reader.GetOrdinal("GuestId")) ? null : reader.GetString(reader.GetOrdinal("GuestId")),
                GuestEmail = reader.IsDBNull(reader.GetOrdinal("GuestEmail")) ? null : reader.GetString(reader.GetOrdinal("GuestEmail")),
                Subject = reader.IsDBNull(reader.GetOrdinal("Subject")) ? "Вопрос по сайту" : reader.GetString(reader.GetOrdinal("Subject")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastMessageAt = reader.GetDateTime(reader.GetOrdinal("LastMessageAt")),
                LastMessagePreview = reader.IsDBNull(reader.GetOrdinal("LastMessagePreview")) ? string.Empty : reader.GetString(reader.GetOrdinal("LastMessagePreview")),
                HasUnreadForAdmin = !reader.IsDBNull(reader.GetOrdinal("HasUnreadForAdmin")) && reader.GetBoolean(reader.GetOrdinal("HasUnreadForAdmin")),
                HasUnreadForUser = !reader.IsDBNull(reader.GetOrdinal("HasUnreadForUser")) && reader.GetBoolean(reader.GetOrdinal("HasUnreadForUser"))
            };
        }

        public async Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId)
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand("sp_Chat_GetMessages", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ConversationId", conversationId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            var result = new List<ChatMessage>();
            while (await reader.ReadAsync())
            {
                result.Add(new ChatMessage
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ConversationId = reader.GetInt32(reader.GetOrdinal("ConversationId")),
                    SenderType = reader.GetString(reader.GetOrdinal("SenderType")),
                    MessageText = reader.GetString(reader.GetOrdinal("MessageText")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            return result;
        }

        public async Task<List<ChatConversation>> GetAdminConversationsAsync()
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand("sp_Chat_GetAdminConversations", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            var result = new List<ChatConversation>();
            while (await reader.ReadAsync())
            {
                result.Add(new ChatConversation
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                    GuestId = reader.IsDBNull(reader.GetOrdinal("GuestId")) ? null : reader.GetString(reader.GetOrdinal("GuestId")),
                    GuestEmail = reader.IsDBNull(reader.GetOrdinal("GuestEmail")) ? null : reader.GetString(reader.GetOrdinal("GuestEmail")),
                    Subject = reader.IsDBNull(reader.GetOrdinal("Subject")) ? "Вопрос по сайту" : reader.GetString(reader.GetOrdinal("Subject")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    LastMessageAt = reader.GetDateTime(reader.GetOrdinal("LastMessageAt")),
                    LastMessagePreview = reader.IsDBNull(reader.GetOrdinal("LastMessagePreview")) ? string.Empty : reader.GetString(reader.GetOrdinal("LastMessagePreview")),
                    HasUnreadForAdmin = !reader.IsDBNull(reader.GetOrdinal("HasUnreadForAdmin")) && reader.GetBoolean(reader.GetOrdinal("HasUnreadForAdmin")),
                    HasUnreadForUser = !reader.IsDBNull(reader.GetOrdinal("HasUnreadForUser")) && reader.GetBoolean(reader.GetOrdinal("HasUnreadForUser"))
                });
            }

            return result;
        }
    }
}
