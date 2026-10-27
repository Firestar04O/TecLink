using Firebase.Auth;
using Firebase.Extensions;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TecLink.UI;
using UnityEngine;
using TecLinkUserProfile = TecLink.Models.UserProfile;

namespace TecLink.Core
{
    [DefaultExecutionOrder(-8000)]
    [DisallowMultipleComponent]
    public sealed class AppManager : MonoBehaviour
    {
        [SerializeField] private NavigationManager navigation;
        [SerializeField] private LoadingPopup loadingPopup;
        [SerializeField] private ErrorPopup errorPopup;
        [SerializeField] private ConfirmPopup confirmPopup;

        public static AppManager Instance { get; private set; }

        public bool IsBusy { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SetBusy(true, "Conectando con Firebase...");

            if (AuthService.Instance == null)
            {
                SetBusy(false);
                ShowError("No se encontró AuthService en la escena.");
                navigation.ShowLogin();
                return;
            }

            AuthService.Instance.ReadinessTask.ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted || !task.Result)
                {
                    SetBusy(false);
                    navigation.ShowLogin();
                    string detail = string.IsNullOrWhiteSpace(FirebaseBootstrap.LastError)
                        ? "No se pudo iniciar Firebase Authentication."
                        : FirebaseBootstrap.LastError;
                    ShowError(detail);
                    return;
                }

                RestoreSession();
            });
        }

        public void SetBusy(bool busy, string message = "Procesando...")
        {
            IsBusy = busy;
            if (busy)
            {
                loadingPopup.Show(message);
            }
            else
            {
                loadingPopup.Hide();
            }
        }

        public void ShowError(string message)
        {
            errorPopup.Show(message);
        }

        public void ShowConfirmation(string message, System.Action onConfirm)
        {
            confirmPopup.Show(message, onConfirm);
        }

        public void RouteAuthenticatedUser(FirebaseUser user)
        {
            if (user == null)
            {
                navigation.ShowLogin();
                return;
            }

            if (!AppConstants.IsInstitutionalEmail(user.Email))
            {
                AuthService.Instance.SignOut();
                navigation.ShowLogin();
                ShowError("La sesión no pertenece al dominio institucional permitido.");
                return;
            }

            if (!user.IsEmailVerified)
            {
                SetBusy(false);
                navigation.ShowVerifyEmail();
                return;
            }

            LoadProfileAndRoute(user);
        }

        public void ShowSavedProfile(TecLinkUserProfile profile)
        {
            if (profile == null)
            {
                ShowError("No se pudo abrir el perfil guardado.");
                return;
            }

            SessionManager.Instance?.SetProfile(profile);
            SetBusy(false);
            navigation.ShowFeed();
        }

        public void ShowProfileEditor()
        {
            navigation.ShowProfileSetup();
        }

        private void RestoreSession()
        {
            FirebaseUser user = AuthService.Instance.CurrentUser;
            if (user == null)
            {
                SetBusy(false);
                navigation.ShowLogin();
                return;
            }

            AuthService.Instance.RefreshCurrentUserAsync().ContinueWithOnMainThread(task =>
            {
                SetBusy(false);
                AuthOperationResult result = task.Result;

                if (!result.Success)
                {
                    AuthService.Instance.SignOut();
                    navigation.ShowLogin();
                    ShowError(result.ErrorMessage);
                    return;
                }

                RouteAuthenticatedUser(result.User);
            });
        }

        private void LoadProfileAndRoute(FirebaseUser user)
        {
            if (FirestoreService.Instance == null)
            {
                SetBusy(false);
                ShowError("No se encontró FirestoreService en la escena.");
                navigation.ShowProfileSetup();
                return;
            }

            SetBusy(true, "Cargando tu perfil...");
            FirestoreService.Instance.ReadinessTask.ContinueWithOnMainThread(readinessTask =>
            {
                if (readinessTask.IsCanceled || readinessTask.IsFaulted || !readinessTask.Result)
                {
                    SetBusy(false);
                    navigation.ShowProfileSetup();
                    ShowError("No se pudo iniciar Cloud Firestore.");
                    return;
                }

                FirestoreService.Instance.LoadUserProfileAsync(user).ContinueWithOnMainThread(loadTask =>
                {
                    SetBusy(false);
                    if (loadTask.IsCanceled || loadTask.IsFaulted)
                    {
                        navigation.ShowProfileSetup();
                        ShowError("No se pudo comprobar tu perfil en Firestore.");
                        return;
                    }

                    ProfileOperationResult result = loadTask.Result;
                    if (!result.Success)
                    {
                        navigation.ShowProfileSetup();
                        ShowError(result.ErrorMessage);
                        return;
                    }

                    if (!result.Exists)
                    {
                        SessionManager.Instance?.SetProfile(null);
                        navigation.ShowProfileSetup();
                        return;
                    }

                    ShowSavedProfile(result.Profile);
                });
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
