using System.Collections.Generic;

namespace TecLink.Models
{
    public sealed class MessageQueryResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public List<ChatMessageRecord> Messages { get; private set; } =
            new List<ChatMessageRecord>();

        public static MessageQueryResult Found(List<ChatMessageRecord> messages)
        {
            return new MessageQueryResult
            {
                Success = true,
                Messages = messages ?? new List<ChatMessageRecord>()
            };
        }

        public static MessageQueryResult Fail(string errorMessage)
        {
            return new MessageQueryResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudieron cargar los mensajes."
                    : errorMessage
            };
        }
    }
}
