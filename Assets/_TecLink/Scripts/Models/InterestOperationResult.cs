namespace TecLink.Models
{
    public sealed class InterestOperationResult
    {
        public bool Success { get; private set; }
        public bool Exists { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public InterestRecord Interest { get; private set; }

        public static InterestOperationResult Found(InterestRecord interest)
        {
            return new InterestOperationResult
            {
                Success = true,
                Exists = interest != null,
                Interest = interest
            };
        }

        public static InterestOperationResult NotFound()
        {
            return new InterestOperationResult { Success = true, Exists = false };
        }

        public static InterestOperationResult Fail(string errorMessage)
        {
            return new InterestOperationResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudo completar la operación con la postulación."
                    : errorMessage
            };
        }
    }
}
