namespace TecLink.Models
{
    public sealed class ChatMessageRecord
    {
        public string Id { get; set; } = string.Empty;
        public ChatMessage Data { get; set; }
    }
}
