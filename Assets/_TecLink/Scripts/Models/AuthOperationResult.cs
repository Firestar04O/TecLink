using Firebase.Auth;

namespace TecLink.Models
{
    public sealed class AuthOperationResult
    {
        private AuthOperationResult(bool success, FirebaseUser user, string errorMessage)
        {
            Success = success;
            User = user;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool Success { get; }

        public FirebaseUser User { get; }

        public string ErrorMessage { get; }

        public static AuthOperationResult Ok(FirebaseUser user)
        {
            return new AuthOperationResult(true, user, string.Empty);
        }

        public static AuthOperationResult Fail(string errorMessage, FirebaseUser user = null)
        {
            return new AuthOperationResult(false, user, errorMessage);
        }
    }
}
