namespace TecLink.Models
{
    public sealed class ChatOperationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        public static ChatOperationResult Ok()
        {
            return new ChatOperationResult { Success = true };
        }

        public static ChatOperationResult Fail(string errorMessage)
        {
            return new ChatOperationResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudo completar la operación con el chat."
                    : errorMessage
            };
        }
    }
}
