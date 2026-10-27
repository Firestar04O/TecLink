using System.Collections.Generic;

namespace TecLink.Models
{
    public sealed class ChatQueryResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public List<ChatRecord> Chats { get; private set; } = new List<ChatRecord>();

        public static ChatQueryResult Found(List<ChatRecord> chats)
        {
            return new ChatQueryResult
            {
                Success = true,
                Chats = chats ?? new List<ChatRecord>()
            };
        }

        public static ChatQueryResult Fail(string errorMessage)
        {
            return new ChatQueryResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudieron cargar los chats."
                    : errorMessage
            };
        }
    }
}
