using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class VerifyEmailController : MonoBehaviour
    {
        [SerializeField] private TMP_Text emailLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button resendButton;
        [SerializeField] private Button alreadyVerifiedButton;
        [SerializeField] private Button logoutButton;

        private bool operationInProgress;

        private void Awake()
        {
            resendButton.onClick.AddListener(OnResendPressed);
            alreadyVerifiedButton.onClick.AddListener(OnAlreadyVerifiedPressed);
            logoutButton.onClick.AddListener(OnLogoutPressed);
        }

        private void OnEnable()
        {
            string email = AuthService.Instance?.CurrentUser?.Email;
            emailLabel.text = string.IsNullOrWhiteSpace(email)
                ? "Correo no disponible"
                : email;
            statusLabel.text = "Abre el enlace que Firebase envió a tu correo. Revisa también Spam.";
        }

        public async void OnResendPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            BeginOperation("Reenviando correo...");
            AuthOperationResult result = await AuthService.Instance.ResendVerificationEmailAsync();
            EndOperation();

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            statusLabel.text = "Correo reenviado. Espera un momento y revisa tu bandeja y Spam.";
        }

        public async void OnAlreadyVerifiedPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            BeginOperation("Comprobando verificación...");
            AuthOperationResult result = await AuthService.Instance.RefreshCurrentUserAsync();
            EndOperation();

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            if (!result.User.IsEmailVerified)
            {
                statusLabel.text = "Firebase todavía muestra el correo como no verificado.";
                AppManager.Instance.ShowError(
                    "Aún no está verificado. Abre el enlace del correo y después vuelve a pulsar este botón.");
                return;
            }

            AppManager.Instance.RouteAuthenticatedUser(result.User);
        }

        public void OnLogoutPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            AuthService.Instance.SignOut();
            NavigationManager.Instance.ShowLogin();
        }

        private void BeginOperation(string message)
        {
            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, message);
        }

        private void EndOperation()
        {
            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            resendButton.interactable = interactable;
            alreadyVerifiedButton.interactable = interactable;
            logoutButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            resendButton.onClick.RemoveListener(OnResendPressed);
            alreadyVerifiedButton.onClick.RemoveListener(OnAlreadyVerifiedPressed);
            logoutButton.onClick.RemoveListener(OnLogoutPressed);
        }
    }
}
