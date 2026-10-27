using Firebase.Firestore;

namespace TecLink.Models
{
    [FirestoreData]
    public sealed class ChatMessage
    {
        [FirestoreProperty("senderId")]
        public string SenderId { get; set; } = string.Empty;

        [FirestoreProperty("text")]
        public string Text { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }
    }
}
