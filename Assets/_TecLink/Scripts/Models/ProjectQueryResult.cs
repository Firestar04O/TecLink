using System.Collections.Generic;

namespace TecLink.Models
{
    public sealed class ProjectQueryResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public List<ProjectRecord> Projects { get; private set; } = new List<ProjectRecord>();

        public static ProjectQueryResult Found(List<ProjectRecord> projects)
        {
            return new ProjectQueryResult
            {
                Success = true,
                Projects = projects ?? new List<ProjectRecord>()
            };
        }

        public static ProjectQueryResult Fail(string errorMessage)
        {
            return new ProjectQueryResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No se pudieron cargar los proyectos."
                    : errorMessage
            };
        }
    }
}
