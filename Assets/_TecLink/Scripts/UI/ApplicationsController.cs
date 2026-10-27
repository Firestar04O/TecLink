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
    public sealed class ApplicationsController : MonoBehaviour
    {
        [SerializeField] private TMP_Text projectTitleLabel;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private RectTransform cardsContent;
        [SerializeField] private InterestCardView cardTemplate;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;

        private readonly List<InterestCardView> cards = new List<InterestCardView>();
        private bool loading;

        private void Awake()
        {
            refreshButton.onClick.AddListener(Refresh);
            backButton.onClick.AddListener(OnBackPressed);
        }

        private void OnEnable()
        {
            ProjectRecord project = SessionManager.Instance?.SelectedProject;
            projectTitleLabel.text = project?.Data?.Title ?? "Proyecto no disponible";
            Refresh();
        }

        public async void Refresh()
        {
            if (loading)
            {
                return;
            }

            ProjectRecord project = SessionManager.Instance?.SelectedProject;
            if (project == null)
            {
                AppManager.Instance.ShowError("No hay un proyecto seleccionado.");
                return;
            }

            loading = true;
            SetInteractable(false);
            AppManager.Instance.SetBusy(true, "Cargando postulaciones...");
            InterestQueryResult result =
                await FirestoreService.Instance.LoadProjectInterestsAsync(project.Id);
            AppManager.Instance.SetBusy(false);
            SetInteractable(true);
            loading = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            ClearCards();
            foreach (InterestRecord interest in result.Interests)
            {
                InterestCardView card = Instantiate(cardTemplate, cardsContent);
                card.gameObject.SetActive(true);
                card.Bind(interest, OnAcceptSelected, OnRejectSelected);
                cards.Add(card);
            }
            emptyLabel.gameObject.SetActive(result.Interests.Count == 0);
        }

        private void OnAcceptSelected(InterestRecord interest)
        {
            AppManager.Instance.ShowConfirmation(
                $"¿Aceptar a {interest.Data.ApplicantName}? Se creará un chat privado para ambos.",
                () => ReviewInterest(interest, true));
        }

        private void OnRejectSelected(InterestRecord interest)
        {
            AppManager.Instance.ShowConfirmation(
                $"¿Rechazar la postulación de {interest.Data.ApplicantName}?",
                () => ReviewInterest(interest, false));
        }

        private async void ReviewInterest(InterestRecord interest, bool accept)
        {
            if (loading)
            {
                return;
            }

            ProjectRecord project = SessionManager.Instance.SelectedProject;
            loading = true;
            SetInteractable(false);
            AppManager.Instance.SetBusy(true, accept
                ? "Aceptando y creando el chat..."
                : "Rechazando postulación...");
            InterestOperationResult result = await FirestoreService.Instance.ReviewInterestAsync(
                project.Id, interest.Data.ApplicantId, accept);

            if (result.Success)
            {
                ProjectOperationResult projectResult =
                    await FirestoreService.Instance.LoadProjectAsync(project.Id);
                if (projectResult.Success)
                {
                    SessionManager.Instance.SelectProject(projectResult.Project, true);
                }
            }

            AppManager.Instance.SetBusy(false);
            SetInteractable(true);
            loading = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            Refresh();
        }

        public void OnBackPressed()
        {
            if (!loading)
            {
                NavigationManager.Instance.ShowProjectDetail();
            }
        }

        private void SetInteractable(bool interactable)
        {
            refreshButton.interactable = interactable;
            backButton.interactable = interactable;
            foreach (InterestCardView card in cards)
            {
                card?.SetInteractable(interactable);
            }
        }

        private void ClearCards()
        {
            foreach (InterestCardView card in cards)
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
            backButton.onClick.RemoveListener(OnBackPressed);
        }
    }
}
