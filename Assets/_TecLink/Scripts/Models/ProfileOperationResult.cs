namespace TecLink.Models
{
    public sealed class ProfileOperationResult
    {
        public bool Success { get; private set; }
        public bool Exists { get; private set; }
        public string ErrorMessage { get; private set; }
        public UserProfile Profile { get; private set; }

        public static ProfileOperationResult Found(UserProfile profile)
        {
            return new ProfileOperationResult
            {
                Success = true,
                Exists = true,
                Profile = profile,
                ErrorMessage = string.Empty
            };
        }

        public static ProfileOperationResult NotFound()
        {
            return new ProfileOperationResult
            {
                Success = true,
                Exists = false,
                ErrorMessage = string.Empty
            };
        }

        public static ProfileOperationResult Fail(string message)
        {
            return new ProfileOperationResult
            {
                Success = false,
                Exists = false,
                ErrorMessage = message ?? string.Empty
            };
        }
    }
}
