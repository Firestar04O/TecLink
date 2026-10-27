using Firebase.Firestore;
using TecLink.Core;

namespace TecLink.Models
{
    [FirestoreData]
    public sealed class ProjectInterest
    {
        [FirestoreProperty("applicantId")]
        public string ApplicantId { get; set; } = string.Empty;

        [FirestoreProperty("applicantName")]
        public string ApplicantName { get; set; } = string.Empty;

        [FirestoreProperty("message")]
        public string Message { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = AppConstants.InterestStatusPending;

        [FirestoreProperty("chatId")]
        public string ChatId { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
