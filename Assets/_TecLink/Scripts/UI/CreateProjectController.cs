using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Firebase.Firestore;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class CreateProjectController : MonoBehaviour
    {
        [SerializeField] private TMP_Text screenTitleLabel;
        [SerializeField] private TMP_Text screenSubtitleLabel;
        [SerializeField] private TMP_Text submitButtonLabel;
        [SerializeField] private TMP_InputField titleInput;
        [SerializeField] private TMP_InputField descriptionInput;
        [SerializeField] private TMP_InputField categoryInput;
        [SerializeField] private TMP_InputField requiredSkillsInput;
        [SerializeField] private TMP_InputField toolsInput;
        [SerializeField] private Button modalityButton;
        [SerializeField] private TMP_Text modalityLabel;
        [SerializeField] private TMP_InputField campusInput;
        [SerializeField] private TMP_InputField hoursPerWeekInput;
        [SerializeField] private TMP_InputField deadlineInput;
        [SerializeField] private Button createButton;
        [SerializeField] private Button cancelButton;

        private bool operationInProgress;
        private int modalityIndex;
        private bool editing;

        private void Awake()
        {
            createButton.onClick.AddListener(OnCreatePressed);
            cancelButton.onClick.AddListener(OnCancelPressed);
            modalityButton.onClick.AddListener(OnModalityPressed);
        }

        private void OnEnable()
        {
            editing = SessionManager.Instance != null &&
                      SessionManager.Instance.IsEditingProject &&
                      SessionManager.Instance.SelectedProject?.Data != null;
            if (editing)
            {
                PopulateForEditing(SessionManager.Instance.SelectedProject.Data);
            }
            else
            {
                ResetForm();
            }

            screenTitleLabel.text = editing ? "Edita tu proyecto" : "Publica un proyecto";
            screenSubtitleLabel.text = editing
                ? "Actualiza la información que verán los estudiantes"
                : "Encuentra estudiantes con las habilidades que necesitas";
            submitButtonLabel.text = editing ? "Guardar cambios" : "Publicar proyecto";
        }

        public async void OnCreatePressed()
        {
            if (operationInProgress)
            {
                return;
            }

            if (!TryBuildProject(out ProjectData project, out string error))
            {
                AppManager.Instance.ShowError(error);
                return;
            }

            if (FirestoreService.Instance == null)
            {
                AppManager.Instance.ShowError("No se encontró el servicio de Firestore.");
                return;
            }

            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, editing
                ? "Guardando cambios..."
                : "Publicando proyecto...");

            ProjectOperationResult result;
            if (editing)
            {
                string projectId = SessionManager.Instance.SelectedProject.Id;
                result = await FirestoreService.Instance.UpdateOwnedProjectAsync(projectId, project);
            }
            else
            {
                result = await FirestoreService.Instance.CreateProjectAsync(project);
            }

            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            if (editing)
            {
                bool fromMyProjects = SessionManager.Instance.ProjectDetailOpenedFromMyProjects;
                SessionManager.Instance.SelectProject(result.Project, fromMyProjects);
                NavigationManager.Instance.ShowProjectDetail();
            }
            else
            {
                NavigationManager.Instance.ShowFeed();
            }
        }

        public void OnCancelPressed()
        {
            if (!operationInProgress)
            {
                if (editing)
                {
                    NavigationManager.Instance.ShowProjectDetail();
                }
                else
                {
                    NavigationManager.Instance.ShowFeed();
                }
            }
        }

        public void OnModalityPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            modalityIndex = (modalityIndex + 1) % 3;
            UpdateModalityLabel();
        }

        private bool TryBuildProject(out ProjectData project, out string error)
        {
            project = null;
            UserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null)
            {
                error = "Debes completar tu perfil antes de publicar un proyecto.";
                return false;
            }

            string title = Clean(titleInput.text);
            string description = Clean(descriptionInput.text);
            string category = Clean(categoryInput.text);
            string campus = Clean(campusInput.text);
            List<string> requiredSkills = ParseList(requiredSkillsInput.text);
            List<string> tools = ParseList(toolsInput.text);

            if (title.Length < AppConstants.MinimumProjectTitleLength ||
                title.Length > AppConstants.MaximumProjectTitleLength)
            {
                error = $"El título debe tener entre {AppConstants.MinimumProjectTitleLength} y " +
                        $"{AppConstants.MaximumProjectTitleLength} caracteres.";
                return false;
            }

            if (description.Length < AppConstants.MinimumProjectDescriptionLength ||
                description.Length > AppConstants.MaximumProjectDescriptionLength)
            {
                error = $"La descripción debe tener entre {AppConstants.MinimumProjectDescriptionLength} y " +
                        $"{AppConstants.MaximumProjectDescriptionLength} caracteres.";
                return false;
            }

            if (category.Length < 2 || category.Length > AppConstants.MaximumProjectCategoryLength)
            {
                error = $"Escribe una categoría válida de hasta " +
                        $"{AppConstants.MaximumProjectCategoryLength} caracteres.";
                return false;
            }

            if (!IsValidList(requiredSkills, 1, AppConstants.MaximumSkills,
                    AppConstants.MaximumSkillLength))
            {
                error = $"Agrega de 1 a {AppConstants.MaximumSkills} habilidades requeridas; " +
                        $"cada una puede tener hasta {AppConstants.MaximumSkillLength} caracteres.";
                return false;
            }

            if (!IsValidList(tools, 0, AppConstants.MaximumProjectTools,
                    AppConstants.MaximumProjectToolLength))
            {
                error = $"Puedes agregar hasta {AppConstants.MaximumProjectTools} herramientas; " +
                        $"cada una puede tener hasta {AppConstants.MaximumProjectToolLength} caracteres.";
                return false;
            }

            if (campus.Length < 2 || campus.Length > AppConstants.MaximumCampusLength)
            {
                error = $"Escribe una sede válida de hasta {AppConstants.MaximumCampusLength} caracteres.";
                return false;
            }

            if (!int.TryParse(Clean(hoursPerWeekInput.text), out int hoursPerWeek) ||
                hoursPerWeek < AppConstants.MinimumProjectHoursPerWeek ||
                hoursPerWeek > AppConstants.MaximumProjectHoursPerWeek)
            {
                error = $"Las horas semanales deben ser un número entre " +
                        $"{AppConstants.MinimumProjectHoursPerWeek} y " +
                        $"{AppConstants.MaximumProjectHoursPerWeek}.";
                return false;
            }

            if (!TryParseDeadline(deadlineInput.text, out Timestamp? deadline, out error))
            {
                return false;
            }

            project = new ProjectData
            {
                OwnerId = profile.Uid,
                OwnerName = profile.DisplayName,
                OwnerCareer = profile.Career,
                Title = title,
                Description = description,
                Category = category,
                RequiredSkills = requiredSkills,
                Tools = tools,
                Modality = ModalityFromDropdown(),
                Campus = campus,
                HoursPerWeek = hoursPerWeek,
                Deadline = deadline,
                Status = AppConstants.ProjectStatusOpen
            };
            error = string.Empty;
            return true;
        }

        private bool TryParseDeadline(string value, out Timestamp? deadline, out string error)
        {
            deadline = null;
            string cleanValue = Clean(value);
            if (cleanValue.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            if (!DateTime.TryParseExact(cleanValue, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date))
            {
                error = "La fecha límite debe usar el formato AAAA-MM-DD, por ejemplo 2026-12-20.";
                return false;
            }

            DateTime utcDeadline = DateTime.SpecifyKind(date.Date.AddHours(12), DateTimeKind.Utc);
            if (date.Date <= DateTime.UtcNow.Date)
            {
                error = "La fecha límite debe ser posterior a hoy.";
                return false;
            }

            deadline = Timestamp.FromDateTime(utcDeadline);
            error = string.Empty;
            return true;
        }

        private string ModalityFromDropdown()
        {
            switch (modalityIndex)
            {
                case 1:
                    return AppConstants.ProjectModalityRemote;
                case 2:
                    return AppConstants.ProjectModalityHybrid;
                default:
                    return AppConstants.ProjectModalityOnSite;
            }
        }

        private void ResetForm()
        {
            titleInput.text = string.Empty;
            descriptionInput.text = string.Empty;
            categoryInput.text = string.Empty;
            requiredSkillsInput.text = string.Empty;
            toolsInput.text = string.Empty;
            modalityIndex = 0;
            UpdateModalityLabel();
            campusInput.text = SessionManager.Instance?.CurrentProfile?.Campus ?? string.Empty;
            hoursPerWeekInput.text = string.Empty;
            deadlineInput.text = string.Empty;
        }

        private void PopulateForEditing(ProjectData project)
        {
            titleInput.text = project.Title;
            descriptionInput.text = project.Description;
            categoryInput.text = project.Category;
            requiredSkillsInput.text = string.Join(", ", project.RequiredSkills ?? new List<string>());
            toolsInput.text = string.Join(", ", project.Tools ?? new List<string>());
            modalityIndex = project.Modality == AppConstants.ProjectModalityRemote
                ? 1
                : project.Modality == AppConstants.ProjectModalityHybrid ? 2 : 0;
            UpdateModalityLabel();
            campusInput.text = project.Campus;
            hoursPerWeekInput.text = project.HoursPerWeek.ToString(CultureInfo.InvariantCulture);
            deadlineInput.text = project.Deadline.HasValue
                ? project.Deadline.Value.ToDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            createButton.interactable = interactable;
            cancelButton.interactable = interactable;
            modalityButton.interactable = interactable;
        }

        private void UpdateModalityLabel()
        {
            switch (modalityIndex)
            {
                case 1:
                    modalityLabel.text = "Remoto";
                    break;
                case 2:
                    modalityLabel.text = "Híbrido";
                    break;
                default:
                    modalityLabel.text = "Presencial";
                    break;
            }
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static List<string> ParseList(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Clean)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsValidList(List<string> values, int minimum, int maximum, int itemLimit)
        {
            return values != null && values.Count >= minimum && values.Count <= maximum &&
                   values.All(value => value.Length > 0 && value.Length <= itemLimit);
        }

        private void OnDestroy()
        {
            createButton.onClick.RemoveListener(OnCreatePressed);
            cancelButton.onClick.RemoveListener(OnCancelPressed);
            modalityButton.onClick.RemoveListener(OnModalityPressed);
        }
    }
}
