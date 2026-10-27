using System.Collections.Generic;
using Firebase.Firestore;

namespace TecLink.Models
{
    [FirestoreData]
    public sealed class UserProfile
    {
        [FirestoreProperty("uid")]
        public string Uid { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [FirestoreProperty("career")]
        public string Career { get; set; } = string.Empty;

        [FirestoreProperty("campus")]
        public string Campus { get; set; } = string.Empty;

        [FirestoreProperty("bio")]
        public string Bio { get; set; } = string.Empty;

        [FirestoreProperty("skills")]
        public List<string> Skills { get; set; } = new List<string>();

        [FirestoreProperty("portfolioUrl")]
        public string PortfolioUrl { get; set; } = string.Empty;

        [FirestoreProperty("avatarId")]
        public int AvatarId { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
