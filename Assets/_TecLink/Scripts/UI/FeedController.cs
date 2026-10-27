using System;
using System.Collections.Generic;
using System.Linq;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class FeedController : MonoBehaviour
    {
        [SerializeField] private TMP_Text greetingLabel;
        [SerializeField] private TMP_InputField filterInput;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private RectTransform cardsContent;
        [SerializeField] private ProjectCardView cardTemplate;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button createProjectButton;
        [SerializeField] private Button myProjectsButton;
        [SerializeField] private Button chatsButton;
        [SerializeField] private Button editProfileButton;
        [SerializeField] private Button logoutButton;

        private readonly List<ProjectCardView> cards = new List<ProjectCardView>();
        private List<ProjectRecord> projects = new List<ProjectRecord>();
        private bool loading;

        private void Awake()
        {
            refreshButton.onClick.AddListener(Refresh);
            createProjectButton.onClick.AddListener(OnCreateProjectPressed);
            myProjectsButton.onClick.AddListener(OnMyProjectsPressed);
            chatsButton.onClick.AddListener(OnChatsPressed);
            editProfileButton.onClick.AddListener(OnEditProfilePressed);
            logoutButton.onClick.AddListener(OnLogoutPressed);
            filterInput.onValueChanged.AddListener(OnFilterChanged);
        }

        private void OnEnable()
        {
            UserProfile profile = SessionManager.Instance?.CurrentProfile;
            greetingLabel.text = profile == null
                ? "Proyectos abiertos"
                : $"Hola, {profile.DisplayName}";
            Refresh();
        }

        public async void Refresh()
        {
            if (loading)
            {
                return;
            }

            loading = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, "Cargando proyectos...");
            ProjectQueryResult result = await FirestoreService.Instance.LoadOpenProjectsAsync();
            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            loading = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            projects = result.Projects;
            RenderProjects();
        }

        private void RenderProjects()
        {
            ClearCards();
            string filter = (filterInput.text ?? string.Empty).Trim();
            List<ProjectRecord> visible = projects
                .Where(record => MatchesFilter(record.Data, filter))
                .ToList();

            foreach (ProjectRecord record in visible)
            {
                ProjectCardView card = Instantiate(cardTemplate, cardsContent);
                card.gameObject.SetActive(true);
                card.Bind(record, OnProjectSelected);
                cards.Add(card);
            }

            emptyLabel.gameObject.SetActive(visible.Count == 0);
            emptyLabel.text = projects.Count == 0
                ? "Todavía no hay proyectos abiertos. ¡Publica el primero!"
                : "Ningún proyecto coincide con el filtro.";
        }

        private static bool MatchesFilter(ProjectData data, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return Contains(data.Title, filter) || Contains(data.Category, filter) ||
                   Contains(data.OwnerCareer, filter) || Contains(data.Campus, filter) ||
                   (data.RequiredSkills ?? new List<string>()).Any(value => Contains(value, filter));
        }

        private static bool Contains(string value, string filter)
        {
            return (value ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnProjectSelected(ProjectRecord record)
        {
            SessionManager.Instance.SelectProject(record, false);
            NavigationManager.Instance.ShowProjectDetail();
        }

        private void OnFilterChanged(string value)
        {
            RenderProjects();
        }

        private void OnCreateProjectPressed()
        {
            SessionManager.Instance.BeginProjectCreation();
            NavigationManager.Instance.ShowCreateProject();
        }

        private void OnMyProjectsPressed()
        {
            NavigationManager.Instance.ShowMyProjects();
        }

        private void OnChatsPressed()
        {
            NavigationManager.Instance.ShowChats();
        }

        private void OnEditProfilePressed()
        {
            AppManager.Instance.ShowProfileEditor();
        }

        private void OnLogoutPressed()
        {
            AuthService.Instance.SignOut();
            NavigationManager.Instance.ShowLogin();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            refreshButton.interactable = interactable;
            createProjectButton.interactable = interactable;
            myProjectsButton.interactable = interactable;
            chatsButton.interactable = interactable;
            editProfileButton.interactable = interactable;
            logoutButton.interactable = interactable;
        }

        private void ClearCards()
        {
            foreach (ProjectCardView card in cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            cards.Clear();
        }

        private void OnDestroy()
        {
            refreshButton.onClick.RemoveListener(Refresh);
            createProjectButton.onClick.RemoveListener(OnCreateProjectPressed);
            myProjectsButton.onClick.RemoveListener(OnMyProjectsPressed);
            chatsButton.onClick.RemoveListener(OnChatsPressed);
            editProfileButton.onClick.RemoveListener(OnEditProfilePressed);
            logoutButton.onClick.RemoveListener(OnLogoutPressed);
            filterInput.onValueChanged.RemoveListener(OnFilterChanged);
        }
    }
}
