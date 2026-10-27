using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TecLink.Core;
using TecLink.Models;
using UnityEngine;

namespace TecLink.FirebaseIntegration
{
    [DefaultExecutionOrder(-8500)]
    [DisallowMultipleComponent]
    public sealed class AuthService : MonoBehaviour
    {
        private readonly TaskCompletionSource<bool> readinessSource =
            new TaskCompletionSource<bool>();

        private FirebaseAuth auth;

        public static AuthService Instance { get; private set; }

        public Task<bool> ReadinessTask => readinessSource.Task;

        public bool IsReady { get; private set; }

        public FirebaseUser CurrentUser => auth?.CurrentUser;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            FirebaseBootstrap.InitializationTask.ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || !task.Result)
                {
                    readinessSource.TrySetResult(false);
                    return;
                }

                try
                {
                    auth = FirebaseAuth.DefaultInstance;
                    IsReady = true;
                    SyncSession();
                    readinessSource.TrySetResult(true);
                    Debug.Log("[TecLink] Firebase Authentication listo.");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[TecLink] No se pudo iniciar Authentication: {exception}");
                    readinessSource.TrySetResult(false);
                }
            });
        }

        public Task<AuthOperationResult> RegisterAndSendVerificationAsync(
            string email,
            string password)
        {
            if (!IsReady)
            {
                return CompletedFailure("Firebase Authentication todavía no está disponible.");
            }

            string normalizedEmail = AppConstants.NormalizeEmail(email);
            if (!AppConstants.IsInstitutionalEmail(normalizedEmail))
            {
                return CompletedFailure(
                    $"Usa un correo institucional que termine exactamente en {AppConstants.InstitutionalEmailSuffix}.");
            }

            var source = new TaskCompletionSource<AuthOperationResult>();

            try
            {
                auth.CreateUserWithEmailAndPasswordAsync(normalizedEmail, password)
                    .ContinueWithOnMainThread(createTask =>
                    {
                        if (!TryGetSuccessfulResult(createTask, out AuthResult authResult, out string error))
                        {
                            source.TrySetResult(AuthOperationResult.Fail(error));
                            return;
                        }

                        FirebaseUser user = authResult.User;
                        SetCurrentUser(user);

                        user.SendEmailVerificationAsync().ContinueWithOnMainThread(verificationTask =>
                        {
                            if (!TryGetSuccessfulResult(verificationTask, out string verificationError))
                            {
                                source.TrySetResult(AuthOperationResult.Fail(
                                    "La cuenta fue creada, pero no se pudo enviar el correo de verificación. " +
                                    verificationError,
                                    user));
                                return;
                            }

                            Debug.Log("[TecLink] Cuenta creada y correo de verificación enviado.");
                            source.TrySetResult(AuthOperationResult.Ok(user));
                        });
                    });
            }
            catch (Exception exception)
            {
                source.TrySetResult(AuthOperationResult.Fail(ToUserMessage(exception)));
            }

            return source.Task;
        }

        public Task<AuthOperationResult> SignInAsync(string email, string password)
        {
            if (!IsReady)
            {
                return CompletedFailure("Firebase Authentication todavía no está disponible.");
            }

            string normalizedEmail = AppConstants.NormalizeEmail(email);
            if (!AppConstants.IsInstitutionalEmail(normalizedEmail))
            {
                return CompletedFailure(
                    $"Usa un correo institucional que termine exactamente en {AppConstants.InstitutionalEmailSuffix}.");
            }

            var source = new TaskCompletionSource<AuthOperationResult>();

            try
            {
                auth.SignInWithEmailAndPasswordAsync(normalizedEmail, password)
                    .ContinueWithOnMainThread(signInTask =>
                    {
                        if (!TryGetSuccessfulResult(signInTask, out AuthResult authResult, out string error))
                        {
                            source.TrySetResult(AuthOperationResult.Fail(error));
                            return;
                        }

                        ReloadAndValidateUser(authResult.User, source);
                    });
            }
            catch (Exception exception)
            {
                source.TrySetResult(AuthOperationResult.Fail(ToUserMessage(exception)));
            }

            return source.Task;
        }

        public Task<AuthOperationResult> RefreshCurrentUserAsync()
        {
            if (!IsReady)
            {
                return CompletedFailure("Firebase Authentication todavía no está disponible.");
            }

            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                return CompletedFailure("No hay una sesión iniciada.");
            }

            var source = new TaskCompletionSource<AuthOperationResult>();
            ReloadAndValidateUser(user, source);
            return source.Task;
        }

        public Task<AuthOperationResult> ResendVerificationEmailAsync()
        {
            if (!IsReady || auth.CurrentUser == null)
            {
                return CompletedFailure("No hay una sesión iniciada.");
            }

            FirebaseUser user = auth.CurrentUser;
            var source = new TaskCompletionSource<AuthOperationResult>();

            try
            {
                user.SendEmailVerificationAsync().ContinueWithOnMainThread(task =>
                {
                    if (!TryGetSuccessfulResult(task, out string error))
                    {
                        source.TrySetResult(AuthOperationResult.Fail(error, user));
                        return;
                    }

                    source.TrySetResult(AuthOperationResult.Ok(user));
                });
            }
            catch (Exception exception)
            {
                source.TrySetResult(AuthOperationResult.Fail(ToUserMessage(exception), user));
            }

            return source.Task;
        }

        public void SignOut()
        {
            auth?.SignOut();
            SessionManager.Instance?.Clear();
            Debug.Log("[TecLink] Sesión cerrada.");
        }

        private void ReloadAndValidateUser(
            FirebaseUser user,
            TaskCompletionSource<AuthOperationResult> source)
        {
            try
            {
                user.ReloadAsync().ContinueWithOnMainThread(reloadTask =>
                {
                    if (!TryGetSuccessfulResult(reloadTask, out string error))
                    {
                        source.TrySetResult(AuthOperationResult.Fail(error));
                        return;
                    }

                    FirebaseUser refreshedUser = auth.CurrentUser;
                    if (refreshedUser == null)
                    {
                        SessionManager.Instance?.Clear();
                        source.TrySetResult(AuthOperationResult.Fail("La sesión ya no es válida. Inicia sesión otra vez."));
                        return;
                    }

                    if (!AppConstants.IsInstitutionalEmail(refreshedUser.Email))
                    {
                        SignOut();
                        source.TrySetResult(AuthOperationResult.Fail(
                            "Esta cuenta no pertenece al dominio institucional permitido."));
                        return;
                    }

                    if (!refreshedUser.IsEmailVerified)
                    {
                        SetCurrentUser(refreshedUser);
                        source.TrySetResult(AuthOperationResult.Ok(refreshedUser));
                        return;
                    }

                    refreshedUser.TokenAsync(true).ContinueWithOnMainThread(tokenTask =>
                    {
                        if (!TryGetSuccessfulResult(tokenTask, out string ignoredToken, out string tokenError))
                        {
                            source.TrySetResult(AuthOperationResult.Fail(tokenError));
                            return;
                        }

                        SetCurrentUser(refreshedUser);
                        source.TrySetResult(AuthOperationResult.Ok(refreshedUser));
                    });
                });
            }
            catch (Exception exception)
            {
                source.TrySetResult(AuthOperationResult.Fail(ToUserMessage(exception)));
            }
        }

        private void SyncSession()
        {
            if (auth.CurrentUser == null)
            {
                SessionManager.Instance?.Clear();
            }
            else
            {
                SetCurrentUser(auth.CurrentUser);
            }
        }

        private static void SetCurrentUser(FirebaseUser user)
        {
            SessionManager.Instance?.SetUser(user);
        }

        private static Task<AuthOperationResult> CompletedFailure(string message)
        {
            return Task.FromResult(AuthOperationResult.Fail(message));
        }

        private static bool TryGetSuccessfulResult<T>(
            Task<T> task,
            out T result,
            out string error)
        {
            result = default;
            if (task.IsCanceled)
            {
                error = "La operación fue cancelada.";
                return false;
            }

            if (task.IsFaulted)
            {
                error = ToUserMessage(task.Exception);
                return false;
            }

            result = task.Result;
            error = string.Empty;
            return true;
        }

        private static bool TryGetSuccessfulResult(Task task, out string error)
        {
            if (task.IsCanceled)
            {
                error = "La operación fue cancelada.";
                return false;
            }

            if (task.IsFaulted)
            {
                error = ToUserMessage(task.Exception);
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string ToUserMessage(Exception exception)
        {
            FirebaseException firebaseException = FindFirebaseException(exception);
            if (firebaseException == null)
            {
                Debug.LogError($"[TecLink] Error de autenticación no reconocido: {exception}");
                return "Ocurrió un error inesperado. Inténtalo otra vez.";
            }

            AuthError authError = (AuthError)firebaseException.ErrorCode;
            Debug.LogWarning(
                $"[TecLink] Firebase Auth error {authError} ({firebaseException.ErrorCode}): " +
                firebaseException.Message);

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    return "El formato del correo no es válido.";
                case AuthError.EmailAlreadyInUse:
                    return "Ese correo ya tiene una cuenta. Prueba iniciar sesión.";
                case AuthError.WeakPassword:
                    return "La contraseña es demasiado débil.";
                case AuthError.WrongPassword:
                case AuthError.UserNotFound:
                case AuthError.InvalidCredential:
                    return "Correo o contraseña incorrectos.";
                case AuthError.UserDisabled:
                    return "Esta cuenta fue deshabilitada.";
                case AuthError.OperationNotAllowed:
                    return "El acceso con correo y contraseña no está habilitado en Firebase.";
                case AuthError.TooManyRequests:
                    return "Hubo demasiados intentos. Espera un momento y vuelve a probar.";
                case AuthError.NetworkRequestFailed:
                    return "No se pudo conectar con Firebase. Revisa tu conexión a Internet.";
                case AuthError.InvalidApiKey:
                case AuthError.AppNotAuthorized:
                    return "TecLink no está autorizado para usar Firebase. Revisa la configuración de la app en Firebase.";
                case AuthError.ApiNotAvailable:
                    return "Firebase Authentication no está disponible en este dispositivo o en esta compilación.";
                case AuthError.MissingEmail:
                    return "Ingresa tu correo institucional.";
                case AuthError.MissingPassword:
                    return "Ingresa una contraseña.";
                case AuthError.QuotaExceeded:
                    return "Firebase alcanzó temporalmente el límite de operaciones. Inténtalo más tarde.";
                case AuthError.InvalidRecipientEmail:
                    return "La cuenta se creó, pero Firebase rechazó la dirección para enviar la verificación.";
                case AuthError.InvalidSender:
                    return "Firebase no tiene configurado correctamente el remitente del correo de verificación.";
                case AuthError.InvalidUserToken:
                case AuthError.UserTokenExpired:
                    return "La sesión de Firebase venció. Inicia sesión nuevamente.";
                case AuthError.Cancelled:
                    return "Firebase canceló la operación. Inténtalo nuevamente.";
                case AuthError.Failure:
                    if (firebaseException.Message.IndexOf(
                            "CONFIGURATION_NOT_FOUND",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "Firebase Authentication todavía no está configurado para este proyecto. " +
                               "Activa Correo electrónico/Contraseña en Firebase Console.";
                    }

                    return "Firebase devolvió un fallo interno. Código: Failure (1).";
                default:
                    return $"Firebase no pudo completar la operación. Código: {authError} ({firebaseException.ErrorCode}).";
            }
        }

        private static FirebaseException FindFirebaseException(Exception exception)
        {
            if (exception is FirebaseException directFirebaseException)
            {
                return directFirebaseException;
            }

            if (exception is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions
                    .Select(FindFirebaseException)
                    .FirstOrDefault(candidate => candidate != null);
            }

            return exception?.InnerException == null
                ? null
                : FindFirebaseException(exception.InnerException);
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
