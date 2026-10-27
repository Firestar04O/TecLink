namespace TecLink.Models
{
    public sealed class ProjectOperationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public string ProjectId { get; private set; } = string.Empty;
        public ProjectRecord Project { get; private set; }

        public static ProjectOperationResult Saved(ProjectRecord project)
        {
            return new ProjectOperationResult
            {
                Success = true,
                ProjectId = project?.Id ?? string.Empty,
                Project = project
            };
        }

        public static ProjectOperationResult Fail(string errorMessage)
        {
            return new ProjectOperationResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudo completar la operación con el proyecto."
                    : errorMessage
            };
        }
    }
}
