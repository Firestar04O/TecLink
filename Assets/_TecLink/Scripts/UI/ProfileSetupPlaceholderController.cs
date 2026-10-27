using TecLink.Core;
using TecLink.FirebaseIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ProfileSetupPlaceholderController : MonoBehaviour
    {
        [SerializeField] private TMP_Text emailLabel;
        [SerializeField] private Button logoutButton;

        private void Awake()
        {
            logoutButton.onClick.AddListener(OnLogoutPressed);
        }

        private void OnEnable()
        {
            string email = AuthService.Instance?.CurrentUser?.Email;
            emailLabel.text = string.IsNullOrWhiteSpace(email)
                ? "Correo verificado"
                : email;
        }

        public void OnLogoutPressed()
        {
            AuthService.Instance.SignOut();
            NavigationManager.Instance.ShowLogin();
        }

        private void OnDestroy()
        {
            logoutButton.onClick.RemoveListener(OnLogoutPressed);
        }
    }
}
