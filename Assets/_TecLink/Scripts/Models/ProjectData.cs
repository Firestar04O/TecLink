using System.Collections.Generic;
using Firebase.Firestore;
using TecLink.Core;

namespace TecLink.Models
{
    [FirestoreData]
    public sealed class ProjectData
    {
        [FirestoreProperty("ownerId")]
        public string OwnerId { get; set; } = string.Empty;

        [FirestoreProperty("ownerName")]
        public string OwnerName { get; set; } = string.Empty;

        [FirestoreProperty("ownerCareer")]
        public string OwnerCareer { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty;

        [FirestoreProperty("requiredSkills")]
        public List<string> RequiredSkills { get; set; } = new List<string>();

        [FirestoreProperty("tools")]
        public List<string> Tools { get; set; } = new List<string>();

        [FirestoreProperty("modality")]
        public string Modality { get; set; } = string.Empty;

        [FirestoreProperty("campus")]
        public string Campus { get; set; } = string.Empty;

        [FirestoreProperty("hoursPerWeek")]
        public int HoursPerWeek { get; set; }

        [FirestoreProperty("deadline")]
        public Timestamp? Deadline { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = AppConstants.ProjectStatusOpen;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
