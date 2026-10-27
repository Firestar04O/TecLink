using System;

namespace TecLink.Core
{
    public static class AppConstants
    {
        public const string InstitutionalDomain = "tecsup.edu.pe";
        public const string InstitutionalEmailSuffix = "@" + InstitutionalDomain;
        public const int MinimumPasswordLength = 8;
        public const int MaximumPasswordLength = 128;
        public const int MaximumEmailLength = 254;
        public const int MinimumDisplayNameLength = 2;
        public const int MaximumDisplayNameLength = 60;
        public const int MaximumCareerLength = 80;
        public const int MaximumCampusLength = 60;
        public const int MaximumBioLength = 500;
        public const int MaximumSkillLength = 40;
        public const int MaximumSkills = 12;
        public const int MaximumPortfolioUrlLength = 300;
        public const int AvailableAvatarCount = 6;
        public const int MinimumProjectTitleLength = 5;
        public const int MaximumProjectTitleLength = 80;
        public const int MinimumProjectDescriptionLength = 20;
        public const int MaximumProjectDescriptionLength = 1500;
        public const int MaximumProjectCategoryLength = 60;
        public const int MaximumProjectToolLength = 40;
        public const int MaximumProjectTools = 12;
        public const int MinimumProjectHoursPerWeek = 1;
        public const int MaximumProjectHoursPerWeek = 80;
        public const int ProjectQueryLimit = 50;
        public const int MinimumInterestMessageLength = 10;
        public const int MaximumInterestMessageLength = 500;
        public const int InterestQueryLimit = 50;
        public const int MaximumChatMessageLength = 1000;
        public const int ChatQueryLimit = 50;
        public const int ChatMessageHistoryLimit = 100;

        public const string ProjectStatusOpen = "open";
        public const string ProjectStatusSelecting = "selecting";
        public const string ProjectStatusFilled = "filled";
        public const string ProjectStatusClosed = "closed";

        public const string ProjectModalityOnSite = "presencial";
        public const string ProjectModalityRemote = "remoto";
        public const string ProjectModalityHybrid = "hibrido";

        public const string InterestStatusPending = "pending";
        public const string InterestStatusAccepted = "accepted";
        public const string InterestStatusRejected = "rejected";

        public static string NormalizeEmail(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        public static bool IsInstitutionalEmail(string value)
        {
            string email = NormalizeEmail(value);
            if (email.Length == 0 || email.Length > MaximumEmailLength)
            {
                return false;
            }

            int atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex != email.LastIndexOf('@'))
            {
                return false;
            }

            if (!email.EndsWith(InstitutionalEmailSuffix, StringComparison.Ordinal))
            {
                return false;
            }

            foreach (char character in email)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
