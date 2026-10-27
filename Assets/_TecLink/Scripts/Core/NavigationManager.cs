using UnityEngine;

namespace TecLink.Core
{
    public enum AppScreen
    {
        Login,
        Register,
        VerifyEmail,
        ProfileSetup,
        Feed,
        CreateProject,
        ProjectDetail,
        MyProjects,
        ApplyProject,
        Applications,
        Chats,
        Chat
    }

    [DisallowMultipleComponent]
    public sealed class NavigationManager : MonoBehaviour
    {
        [SerializeField] private GameObject loginScreen;
        [SerializeField] private GameObject registerScreen;
        [SerializeField] private GameObject verifyEmailScreen;
        [SerializeField] private GameObject profileSetupScreen;
        [SerializeField] private GameObject feedScreen;
        [SerializeField] private GameObject createProjectScreen;
        [SerializeField] private GameObject projectDetailScreen;
        [SerializeField] private GameObject myProjectsScreen;
        [SerializeField] private GameObject applyProjectScreen;
        [SerializeField] private GameObject applicationsScreen;
        [SerializeField] private GameObject chatsScreen;
        [SerializeField] private GameObject chatScreen;

        public static NavigationManager Instance { get; private set; }

        public AppScreen CurrentScreen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ShowLogin()
        {
            Show(AppScreen.Login);
        }

        public void ShowRegister()
        {
            Show(AppScreen.Register);
        }

        public void ShowVerifyEmail()
        {
            Show(AppScreen.VerifyEmail);
        }

        public void ShowProfileSetup()
        {
            Show(AppScreen.ProfileSetup);
        }

        public void ShowFeed()
        {
            Show(AppScreen.Feed);
        }

        public void ShowCreateProject()
        {
            Show(AppScreen.CreateProject);
        }

        public void ShowProjectDetail()
        {
            Show(AppScreen.ProjectDetail);
        }

        public void ShowMyProjects()
        {
            Show(AppScreen.MyProjects);
        }

        public void ShowApplyProject()
        {
            Show(AppScreen.ApplyProject);
        }

        public void ShowApplications()
        {
            Show(AppScreen.Applications);
        }

        public void ShowChats()
        {
            Show(AppScreen.Chats);
        }

        public void ShowChat()
        {
            Show(AppScreen.Chat);
        }

        public void Show(AppScreen screen)
        {
            SetActive(loginScreen, screen == AppScreen.Login);
            SetActive(registerScreen, screen == AppScreen.Register);
            SetActive(verifyEmailScreen, screen == AppScreen.VerifyEmail);
            SetActive(profileSetupScreen, screen == AppScreen.ProfileSetup);
            SetActive(feedScreen, screen == AppScreen.Feed);
            SetActive(createProjectScreen, screen == AppScreen.CreateProject);
            SetActive(projectDetailScreen, screen == AppScreen.ProjectDetail);
            SetActive(myProjectsScreen, screen == AppScreen.MyProjects);
            SetActive(applyProjectScreen, screen == AppScreen.ApplyProject);
            SetActive(applicationsScreen, screen == AppScreen.Applications);
            SetActive(chatsScreen, screen == AppScreen.Chats);
            SetActive(chatScreen, screen == AppScreen.Chat);
            CurrentScreen = screen;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
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
