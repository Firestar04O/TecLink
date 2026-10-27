using System.Collections.Generic;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ProjectDetailController : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text ownerLabel;
        [SerializeField] private TMP_Text metadataLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text skillsLabel;
        [SerializeField] private TMP_Text toolsLabel;
        [SerializeField] private TMP_Text deadlineLabel;
        [SerializeField] private TMP_Text actionHintLabel;
        [SerializeField] private Button editButton;
        [SerializeField] private Button closeProjectButton;
        [SerializeField] private Button interestButton;
        [SerializeField] private Button applicationsButton;
        [SerializeField] private Button backButton;

        private bool operationInProgress;
        private int viewVersion;

        private void Awake()
        {
            editButton.onClick.AddListener(OnEditPressed);
            closeProjectButton.onClick.AddListener(OnClosePressed);
            interestButton.onClick.AddListener(OnInterestPressed);
            applicationsButton.onClick.AddListener(OnApplicationsPressed);
            backButton.onClick.AddListener(OnBackPressed);
        }

        private void OnEnable()
        {
            viewVersion++;
            ShowSelectedProject();
        }

        private void OnDisable()
        {
            viewVersion++;
        }

        private void ShowSelectedProject()
        {
            ProjectRecord record = SessionManager.Instance?.SelectedProject;
            if (record?.Data == null)
            {
                AppManager.Instance.ShowError("No se pudo abrir la publicación seleccionada.");
                NavigationManager.Instance.ShowFeed();
                return;
            }

            ProjectData data = record.Data;
            titleLabel.text = data.Title;
            statusLabel.text = ProjectCardView.DisplayStatus(data.Status);
            ownerLabel.text = string.IsNullOrWhiteSpace(data.OwnerCareer)
                ? $"Publicado por {data.OwnerName}"
                : $"Publicado por {data.OwnerName} · {data.OwnerCareer}";
            metadataLabel.text = $"{data.Category} · {ProjectCardView.DisplayModality(data.Modality)} · " +
                                 $"{data.Campus} · {data.HoursPerWeek} h/sem";
            descriptionLabel.text = data.Description;
            skillsLabel.text = $"Habilidades requeridas:\n" +
                               $"{string.Join(", ", data.RequiredSkills ?? new List<string>())}";
            toolsLabel.text = data.Tools == null || data.Tools.Count == 0
                ? "Herramientas: no especificadas"
                : $"Herramientas: {string.Join(", ", data.Tools)}";
            deadlineLabel.text = data.Deadline.HasValue
                ? $"Fecha límite: {data.Deadline.Value.ToDateTime().ToLocalTime():yyyy-MM-dd}"
                : "Fecha límite: no especificada";

            string currentUserId = AuthService.Instance?.CurrentUser?.UserId;
            bool isOwner = !string.IsNullOrWhiteSpace(currentUserId) && data.OwnerId == currentUserId;
            bool canModify = isOwner && data.Status != AppConstants.ProjectStatusClosed;
            editButton.gameObject.SetActive(canModify);
            closeProjectButton.gameObject.SetActive(canModify);
            applicationsButton.gameObject.SetActive(isOwner);
            interestButton.gameObject.SetActive(!isOwner && data.Status == AppConstants.ProjectStatusOpen);
            interestButton.interactable = false;
            actionHintLabel.text = isOwner
                ? (canModify
                    ? "Puedes editar, cerrar o revisar las postulaciones."
                    : "Puedes revisar el historial de postulaciones de este proyecto.")
                : data.Status == AppConstants.ProjectStatusOpen
                    ? "Comprobando si ya enviaste una postulación..."
                    : "Esta publicación ya no acepta nuevas postulaciones.";

            if (!isOwner && data.Status == AppConstants.ProjectStatusOpen)
            {
                RefreshInterestStatus(record.Id, viewVersion);
            }
        }

        private async void RefreshInterestStatus(string projectId, int requestedVersion)
        {
            InterestOperationResult result =
                await FirestoreService.Instance.LoadCurrentUserInterestAsync(projectId);
            if (requestedVersion != viewVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (!result.Success)
            {
                interestButton.gameObject.SetActive(false);
                actionHintLabel.text = "No se pudo comprobar tu postulación.";
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            if (!result.Exists)
            {
                interestButton.gameObject.SetActive(true);
                interestButton.interactable = true;
                actionHintLabel.text = "¿Puedes aportar al proyecto? Envía una presentación breve.";
                return;
            }

            interestButton.gameObject.SetActive(false);
            actionHintLabel.text = result.Interest.Data.Status == AppConstants.InterestStatusAccepted
                ? "Tu postulación fue aceptada. El chat ya fue creado."
                : result.Interest.Data.Status == AppConstants.InterestStatusRejected
                    ? "Tu postulación fue rechazada."
                    : "Tu postulación está pendiente de respuesta.";
        }

        public void OnInterestPressed()
        {
            if (!operationInProgress)
            {
                NavigationManager.Instance.ShowApplyProject();
            }
        }

        public void OnApplicationsPressed()
        {
            if (!operationInProgress)
            {
                NavigationManager.Instance.ShowApplications();
            }
        }

        public void OnEditPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            SessionManager.Instance.BeginProjectEditing(SessionManager.Instance.SelectedProject);
            NavigationManager.Instance.ShowCreateProject();
        }

        public void OnClosePressed()
        {
            if (operationInProgress)
            {
                return;
            }

            AppManager.Instance.ShowConfirmation(
                "¿Cerrar esta publicación? Dejará de aparecer en el feed público y no podrá reabrirse.",
                CloseSelectedProject);
        }

        public void OnBackPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            if (SessionManager.Instance.ProjectDetailOpenedFromMyProjects)
            {
                NavigationManager.Instance.ShowMyProjects();
            }
            else
            {
                NavigationManager.Instance.ShowFeed();
            }
        }

        private async void CloseSelectedProject()
        {
            ProjectRecord record = SessionManager.Instance?.SelectedProject;
            if (record == null)
            {
                AppManager.Instance.ShowError("No hay una publicación seleccionada.");
                return;
            }

            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, "Cerrando publicación...");
            ProjectOperationResult result =
                await FirestoreService.Instance.CloseOwnedProjectAsync(record.Id);
            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            SessionManager.Instance.SelectProject(result.Project, true);
            NavigationManager.Instance.ShowMyProjects();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            editButton.interactable = interactable;
            closeProjectButton.interactable = interactable;
            interestButton.interactable = interactable;
            applicationsButton.interactable = interactable;
            backButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            editButton.onClick.RemoveListener(OnEditPressed);
            closeProjectButton.onClick.RemoveListener(OnClosePressed);
            interestButton.onClick.RemoveListener(OnInterestPressed);
            applicationsButton.onClick.RemoveListener(OnApplicationsPressed);
            backButton.onClick.RemoveListener(OnBackPressed);
        }
    }
}
