using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

namespace TecLink.FirebaseIntegration
{
    /// <summary>
    /// Initializes the default Firebase application once and keeps it alive
    /// while scenes change. No other Firebase service should be used until
    /// InitializationTask completes with true or IsReady becomes true.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class FirebaseBootstrap : MonoBehaviour
    {
        private static TaskCompletionSource<bool> initializationSource =
            CreateInitializationSource();

        public static FirebaseBootstrap Instance { get; private set; }

        public static FirebaseApp App { get; private set; }

        public static bool IsReady { get; private set; }

        public static string LastError { get; private set; } = string.Empty;

        public static Task<bool> InitializationTask => initializationSource.Task;

        public static event Action<bool, string> InitializationCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            App = null;
            IsReady = false;
            LastError = string.Empty;
            InitializationCompleted = null;
            initializationSource = CreateInitializationSource();
        }

        private static TaskCompletionSource<bool> CreateInitializationSource()
        {
            return new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            Debug.Log(
                $"[TecLink] Comprobando dependencias de Firebase. " +
                $"Plataforma: {Application.platform}; paquete: {Application.identifier}");

            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync()
                    .ContinueWithOnMainThread(HandleDependencyCheckCompleted);
            }
            catch (Exception exception)
            {
                CompleteInitializationWithError(
                    "No se pudo iniciar la comprobación de dependencias de Firebase.",
                    exception);
            }
        }

        private void HandleDependencyCheckCompleted(Task<DependencyStatus> task)
        {
            if (task.IsCanceled)
            {
                CompleteInitializationWithError(
                    "La comprobación de dependencias de Firebase fue cancelada.");
                return;
            }

            if (task.IsFaulted)
            {
                CompleteInitializationWithError(
                    "Firebase produjo una excepción al comprobar sus dependencias.",
                    task.Exception?.GetBaseException());
                return;
            }

            DependencyStatus status = task.Result;
            if (status != DependencyStatus.Available)
            {
                CompleteInitializationWithError(
                    $"Las dependencias de Firebase no están disponibles: {status}.");
                return;
            }

            try
            {
                App = FirebaseApp.DefaultInstance;
                IsReady = true;
                LastError = string.Empty;
                initializationSource.TrySetResult(true);

                string successMessage =
                    $"Firebase inicializado correctamente. App: {App.Name}; " +
                    $"paquete: {Application.identifier}.";

                Debug.Log($"[TecLink] {successMessage}");
                InitializationCompleted?.Invoke(true, successMessage);
            }
            catch (Exception exception)
            {
                CompleteInitializationWithError(
                    "Las dependencias están disponibles, pero no se pudo crear FirebaseApp.",
                    exception);
            }
        }

        private static void CompleteInitializationWithError(
            string message,
            Exception exception = null)
        {
            IsReady = false;
            App = null;
            LastError = exception == null
                ? message
                : $"{message} {exception.GetType().Name}: {exception.Message}";

            initializationSource.TrySetResult(false);
            Debug.LogError($"[TecLink] {LastError}");
            InitializationCompleted?.Invoke(false, LastError);
        }
    }
}
