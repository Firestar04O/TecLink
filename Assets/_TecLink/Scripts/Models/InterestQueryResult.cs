using System.Collections.Generic;

namespace TecLink.Models
{
    public sealed class InterestQueryResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public List<InterestRecord> Interests { get; private set; } = new List<InterestRecord>();

        public static InterestQueryResult Found(List<InterestRecord> interests)
        {
            return new InterestQueryResult
            {
                Success = true,
                Interests = interests ?? new List<InterestRecord>()
            };
        }

        public static InterestQueryResult Fail(string errorMessage)
        {
            return new InterestQueryResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudieron cargar las postulaciones."
                    : errorMessage
            };
        }
    }
}
