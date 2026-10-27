using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ApplyProjectController : MonoBehaviour
    {
        [SerializeField] private TMP_Text projectTitleLabel;
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private TMP_Text characterCountLabel;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;

        private bool operationInProgress;

        private void Awake()
        {
            submitButton.onClick.AddListener(OnSubmitPressed);
            cancelButton.onClick.AddListener(OnCancelPressed);
            messageInput.onValueChanged.AddListener(OnMessageChanged);
        }

        private void OnEnable()
        {
            ProjectRecord project = SessionManager.Instance?.SelectedProject;
            if (project?.Data == null)
            {
                AppManager.Instance.ShowError("No se pudo recuperar el proyecto seleccionado.");
                NavigationManager.Instance.ShowFeed();
                return;
            }

            projectTitleLabel.text = project.Data.Title;
            messageInput.text = string.Empty;
            UpdateCharacterCount();
        }

        public async void OnSubmitPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            ProjectRecord project = SessionManager.Instance?.SelectedProject;
            if (project == null)
            {
                AppManager.Instance.ShowError("No hay un proyecto seleccionado.");
                return;
            }

            string message = (messageInput.text ?? string.Empty).Trim();
            if (message.Length < AppConstants.MinimumInterestMessageLength ||
                message.Length > AppConstants.MaximumInterestMessageLength)
            {
                AppManager.Instance.ShowError(
                    $"Tu presentación debe tener entre {AppConstants.MinimumInterestMessageLength} y " +
                    $"{AppConstants.MaximumInterestMessageLength} caracteres.");
                return;
            }

            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, "Enviando postulación...");
            InterestOperationResult result =
                await FirestoreService.Instance.CreateInterestAsync(project.Id, message);
            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            NavigationManager.Instance.ShowProjectDetail();
        }

        public void OnCancelPressed()
        {
            if (!operationInProgress)
            {
                NavigationManager.Instance.ShowProjectDetail();
            }
        }

        private void OnMessageChanged(string value)
        {
            UpdateCharacterCount();
        }

        private void UpdateCharacterCount()
        {
            int count = (messageInput.text ?? string.Empty).Length;
            characterCountLabel.text = $"{count}/{AppConstants.MaximumInterestMessageLength}";
        }

        private void SetButtonsInteractable(bool interactable)
        {
            submitButton.interactable = interactable;
            cancelButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            submitButton.onClick.RemoveListener(OnSubmitPressed);
            cancelButton.onClick.RemoveListener(OnCancelPressed);
            messageInput.onValueChanged.RemoveListener(OnMessageChanged);
        }
    }
}
