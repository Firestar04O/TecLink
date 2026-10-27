using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class LoginController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button openRegisterButton;

        private bool operationInProgress;

        private void Awake()
        {
            loginButton.onClick.AddListener(OnLoginPressed);
            openRegisterButton.onClick.AddListener(OnOpenRegisterPressed);
        }

        public async void OnLoginPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            string email = AppConstants.NormalizeEmail(emailInput.text);
            string password = passwordInput.text ?? string.Empty;

            if (!AppConstants.IsInstitutionalEmail(email))
            {
                AppManager.Instance.ShowError(
                    $"Ingresa un correo que termine exactamente en {AppConstants.InstitutionalEmailSuffix}.");
                return;
            }

            if (password.Length == 0)
            {
                AppManager.Instance.ShowError("Ingresa tu contraseña.");
                return;
            }

            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, "Iniciando sesión...");

            AuthOperationResult result = await AuthService.Instance.SignInAsync(email, password);

            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            passwordInput.text = string.Empty;
            AppManager.Instance.RouteAuthenticatedUser(result.User);
        }

        public void OnOpenRegisterPressed()
        {
            if (!operationInProgress)
            {
                passwordInput.text = string.Empty;
                NavigationManager.Instance.ShowRegister();
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            loginButton.interactable = interactable;
            openRegisterButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            loginButton.onClick.RemoveListener(OnLoginPressed);
            openRegisterButton.onClick.RemoveListener(OnOpenRegisterPressed);
        }
    }
}
