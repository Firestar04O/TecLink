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
    public sealed class MyProjectsController : MonoBehaviour
    {
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private RectTransform cardsContent;
        [SerializeField] private ProjectCardView cardTemplate;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button backButton;

        private readonly List<ProjectCardView> cards = new List<ProjectCardView>();
        private bool loading;

        private void Awake()
        {
            refreshButton.onClick.AddListener(Refresh);
            createButton.onClick.AddListener(OnCreatePressed);
            backButton.onClick.AddListener(OnBackPressed);
        }

        private void OnEnable()
        {
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
            AppManager.Instance.SetBusy(true, "Cargando tus proyectos...");
            ProjectQueryResult result = await FirestoreService.Instance.LoadOwnedProjectsAsync();
            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            loading = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            ClearCards();
            foreach (ProjectRecord record in result.Projects)
            {
                ProjectCardView card = Instantiate(cardTemplate, cardsContent);
                card.gameObject.SetActive(true);
                card.Bind(record, OnProjectSelected);
                cards.Add(card);
            }

            emptyLabel.gameObject.SetActive(result.Projects.Count == 0);
        }

        private void OnProjectSelected(ProjectRecord record)
        {
            SessionManager.Instance.SelectProject(record, true);
            NavigationManager.Instance.ShowProjectDetail();
        }

        private void OnCreatePressed()
        {
            SessionManager.Instance.BeginProjectCreation();
            NavigationManager.Instance.ShowCreateProject();
        }

        private void OnBackPressed()
        {
            NavigationManager.Instance.ShowFeed();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            refreshButton.interactable = interactable;
            createButton.interactable = interactable;
            backButton.interactable = interactable;
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
            createButton.onClick.RemoveListener(OnCreatePressed);
            backButton.onClick.RemoveListener(OnBackPressed);
        }
    }
}
