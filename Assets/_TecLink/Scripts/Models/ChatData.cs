using System.Collections.Generic;
using Firebase.Firestore;

namespace TecLink.Models
{
    [FirestoreData]
    public sealed class ChatData
    {
        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("projectTitle")]
        public string ProjectTitle { get; set; } = string.Empty;

        [FirestoreProperty("ownerId")]
        public string OwnerId { get; set; } = string.Empty;

        [FirestoreProperty("applicantId")]
        public string ApplicantId { get; set; } = string.Empty;

        [FirestoreProperty("memberIds")]
        public List<string> MemberIds { get; set; } = new List<string>();

        [FirestoreProperty("lastMessage")]
        public string LastMessage { get; set; } = string.Empty;

        [FirestoreProperty("lastMessageAt")]
        public Timestamp LastMessageAt { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }
    }
}
