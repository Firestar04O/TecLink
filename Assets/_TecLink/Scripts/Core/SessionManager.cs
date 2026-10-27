using Firebase.Auth;
using TecLink.Models;
using UnityEngine;
using TecLinkUserProfile = TecLink.Models.UserProfile;

namespace TecLink.Core
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public FirebaseUser CurrentUser { get; private set; }

        public TecLinkUserProfile CurrentProfile { get; private set; }

        public ProjectRecord SelectedProject { get; private set; }

        public ChatRecord SelectedChat { get; private set; }

        public bool IsEditingProject { get; private set; }

        public bool ProjectDetailOpenedFromMyProjects { get; private set; }

        public bool IsSignedIn => CurrentUser != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetUser(FirebaseUser user)
        {
            CurrentUser = user;
        }

        public void SetProfile(TecLinkUserProfile profile)
        {
            CurrentProfile = profile;
        }

        public void BeginProjectCreation()
        {
            SelectedProject = null;
            IsEditingProject = false;
            ProjectDetailOpenedFromMyProjects = false;
        }

        public void SelectProject(ProjectRecord project, bool fromMyProjects = false)
        {
            SelectedProject = project;
            IsEditingProject = false;
            ProjectDetailOpenedFromMyProjects = fromMyProjects;
        }

        public void BeginProjectEditing(ProjectRecord project)
        {
            SelectedProject = project;
            IsEditingProject = project != null;
        }

        public void SelectChat(ChatRecord chat)
        {
            SelectedChat = chat;
        }

        public void Clear()
        {
            CurrentUser = null;
            CurrentProfile = null;
            SelectedProject = null;
            SelectedChat = null;
            IsEditingProject = false;
            ProjectDetailOpenedFromMyProjects = false;
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
