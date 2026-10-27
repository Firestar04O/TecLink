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
    public sealed class FeedPlaceholderController : MonoBehaviour
    {
        [SerializeField] private TMP_Text greetingLabel;
        [SerializeField] private TMP_Text profileSummaryLabel;
        [SerializeField] private TMP_Text phaseStatusLabel;
        [SerializeField] private Button createProjectButton;
        [SerializeField] private Button editProfileButton;
        [SerializeField] private Button logoutButton;

        private string creationMessage = string.Empty;

        public static FeedPlaceholderController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            createProjectButton.onClick.AddListener(OnCreateProjectPressed);
            editProfileButton.onClick.AddListener(OnEditProfilePressed);
            logoutButton.onClick.AddListener(OnLogoutPressed);
        }

        private void OnEnable()
        {
            UserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null)
            {
                greetingLabel.text = "Perfil no disponible";
                profileSummaryLabel.text = "Vuelve a iniciar sesión para recuperar tu perfil.";
                return;
            }

            greetingLabel.text = $"¡Hola, {profile.DisplayName}!";
            profileSummaryLabel.text =
                $"{profile.Career} · {profile.Campus}\n" +
                $"Habilidades: {string.Join(", ", profile.Skills ?? new List<string>())}";

            phaseStatusLabel.text = string.IsNullOrWhiteSpace(creationMessage)
                ? "Fase 4A activa: ya puedes publicar un proyecto.\nEl feed completo llegará en la fase 4B."
                : creationMessage;
        }

        public void ShowProjectCreated(string title, string projectId)
        {
            string shortId = string.IsNullOrWhiteSpace(projectId)
                ? string.Empty
                : $"\nID: {projectId}";
            creationMessage = $"Proyecto «{title}» publicado correctamente.{shortId}";
        }

        public void OnCreateProjectPressed()
        {
            NavigationManager.Instance.ShowCreateProject();
        }

        public void OnEditProfilePressed()
        {
            AppManager.Instance.ShowProfileEditor();
        }

        public void OnLogoutPressed()
        {
            AuthService.Instance.SignOut();
            NavigationManager.Instance.ShowLogin();
        }

        private void OnDestroy()
        {
            createProjectButton.onClick.RemoveListener(OnCreateProjectPressed);
            editProfileButton.onClick.RemoveListener(OnEditProfilePressed);
            logoutButton.onClick.RemoveListener(OnLogoutPressed);

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
