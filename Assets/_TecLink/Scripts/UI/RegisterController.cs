using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class RegisterController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField confirmPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button backToLoginButton;

        private bool operationInProgress;

        private void Awake()
        {
            registerButton.onClick.AddListener(OnRegisterPressed);
            backToLoginButton.onClick.AddListener(OnBackToLoginPressed);
        }

        public async void OnRegisterPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            string email = AppConstants.NormalizeEmail(emailInput.text);
            string password = passwordInput.text ?? string.Empty;
            string confirmation = confirmPasswordInput.text ?? string.Empty;

            string validationError = Validate(email, password, confirmation);
            if (!string.IsNullOrEmpty(validationError))
            {
                AppManager.Instance.ShowError(validationError);
                return;
            }

            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, "Creando tu cuenta...");

            AuthOperationResult result =
                await AuthService.Instance.RegisterAndSendVerificationAsync(email, password);

            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;

            passwordInput.text = string.Empty;
            confirmPasswordInput.text = string.Empty;

            if (!result.Success)
            {
                if (result.User != null)
                {
                    NavigationManager.Instance.ShowVerifyEmail();
                }

                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            NavigationManager.Instance.ShowVerifyEmail();
        }

        public void OnBackToLoginPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            passwordInput.text = string.Empty;
            confirmPasswordInput.text = string.Empty;
            NavigationManager.Instance.ShowLogin();
        }

        private static string Validate(string email, string password, string confirmation)
        {
            if (!AppConstants.IsInstitutionalEmail(email))
            {
                return $"Usa un correo que termine exactamente en {AppConstants.InstitutionalEmailSuffix}.";
            }

            if (password != password.Trim())
            {
                return "La contraseña no puede comenzar ni terminar con espacios.";
            }

            if (password.Length < AppConstants.MinimumPasswordLength)
            {
                return $"La contraseña debe tener al menos {AppConstants.MinimumPasswordLength} caracteres.";
            }

            if (password.Length > AppConstants.MaximumPasswordLength)
            {
                return $"La contraseña no puede superar {AppConstants.MaximumPasswordLength} caracteres.";
            }

            if (password != confirmation)
            {
                return "Las contraseñas no coinciden.";
            }

            return string.Empty;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            registerButton.interactable = interactable;
            backToLoginButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            registerButton.onClick.RemoveListener(OnRegisterPressed);
            backToLoginButton.onClick.RemoveListener(OnBackToLoginPressed);
        }
    }
}
