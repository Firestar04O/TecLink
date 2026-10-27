using System;
using System.Collections.Generic;
using System.Linq;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ProfileController : MonoBehaviour
    {
        private static readonly Color[] AvatarColors =
        {
            Hex("16B8A6"), Hex("4F8EF7"), Hex("A66CFF"),
            Hex("FF8A5B"), Hex("E14F8B"), Hex("66A847")
        };

        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text emailLabel;
        [SerializeField] private Image avatarPreview;
        [SerializeField] private TMP_Text avatarPreviewLabel;
        [SerializeField] private TMP_Text avatarIndexLabel;
        [SerializeField] private TMP_InputField displayNameInput;
        [SerializeField] private TMP_InputField careerInput;
        [SerializeField] private TMP_InputField campusInput;
        [SerializeField] private TMP_InputField bioInput;
        [SerializeField] private TMP_InputField skillsInput;
        [SerializeField] private TMP_InputField portfolioUrlInput;
        [SerializeField] private Button previousAvatarButton;
        [SerializeField] private Button nextAvatarButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;

        private int avatarId;
        private bool operationInProgress;
        private bool editingExistingProfile;

        private void Awake()
        {
            previousAvatarButton.onClick.AddListener(OnPreviousAvatarPressed);
            nextAvatarButton.onClick.AddListener(OnNextAvatarPressed);
            saveButton.onClick.AddListener(OnSavePressed);
            cancelButton.onClick.AddListener(OnCancelPressed);
            displayNameInput.onValueChanged.AddListener(OnDisplayNameChanged);
        }

        private void OnEnable()
        {
            UserProfile profile = SessionManager.Instance?.CurrentProfile;
            editingExistingProfile = profile != null;
            titleLabel.text = editingExistingProfile ? "Edita tu perfil" : "Completa tu perfil";
            cancelButton.gameObject.SetActive(editingExistingProfile);

            string authEmail = AuthService.Instance?.CurrentUser?.Email;
            emailLabel.text = string.IsNullOrWhiteSpace(authEmail)
                ? "Correo no disponible"
                : authEmail;

            if (profile == null)
            {
                displayNameInput.text = string.Empty;
                careerInput.text = string.Empty;
                campusInput.text = string.Empty;
                bioInput.text = string.Empty;
                skillsInput.text = string.Empty;
                portfolioUrlInput.text = string.Empty;
                avatarId = 0;
            }
            else
            {
                displayNameInput.text = profile.DisplayName;
                careerInput.text = profile.Career;
                campusInput.text = profile.Campus;
                bioInput.text = profile.Bio;
                skillsInput.text = string.Join(", ", profile.Skills ?? new List<string>());
                portfolioUrlInput.text = profile.PortfolioUrl;
                avatarId = Mathf.Clamp(profile.AvatarId, 0, AppConstants.AvailableAvatarCount - 1);
            }

            UpdateAvatarPreview();
        }

        public void OnPreviousAvatarPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            avatarId = (avatarId - 1 + AppConstants.AvailableAvatarCount) %
                       AppConstants.AvailableAvatarCount;
            UpdateAvatarPreview();
        }

        public void OnNextAvatarPressed()
        {
            if (operationInProgress)
            {
                return;
            }

            avatarId = (avatarId + 1) % AppConstants.AvailableAvatarCount;
            UpdateAvatarPreview();
        }

        public async void OnSavePressed()
        {
            if (operationInProgress)
            {
                return;
            }

            Firebase.Auth.FirebaseUser user = AuthService.Instance?.CurrentUser;
            if (user == null)
            {
                AppManager.Instance.ShowError("La sesión terminó. Inicia sesión nuevamente.");
                return;
            }

            if (!TryBuildProfile(user.UserId, user.Email, out UserProfile profile, out string error))
            {
                AppManager.Instance.ShowError(error);
                return;
            }

            operationInProgress = true;
            SetButtonsInteractable(false);
            AppManager.Instance.SetBusy(true, "Guardando tu perfil...");

            ProfileOperationResult result =
                await FirestoreService.Instance.SaveCurrentUserProfileAsync(profile);

            AppManager.Instance.SetBusy(false);
            SetButtonsInteractable(true);
            operationInProgress = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            AppManager.Instance.ShowSavedProfile(result.Profile);
        }

        public void OnCancelPressed()
        {
            if (!operationInProgress && editingExistingProfile)
            {
                NavigationManager.Instance.ShowFeed();
            }
        }

        private bool TryBuildProfile(
            string uid,
            string email,
            out UserProfile profile,
            out string error)
        {
            profile = null;
            string displayName = (displayNameInput.text ?? string.Empty).Trim();
            string career = (careerInput.text ?? string.Empty).Trim();
            string campus = (campusInput.text ?? string.Empty).Trim();
            string bio = (bioInput.text ?? string.Empty).Trim();
            string portfolioUrl = (portfolioUrlInput.text ?? string.Empty).Trim();

            List<string> skills = (skillsInput.text ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(skill => skill.Trim())
                .Where(skill => skill.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (displayName.Length < AppConstants.MinimumDisplayNameLength ||
                displayName.Length > AppConstants.MaximumDisplayNameLength)
            {
                error = $"El nombre debe tener entre {AppConstants.MinimumDisplayNameLength} y " +
                        $"{AppConstants.MaximumDisplayNameLength} caracteres.";
                return false;
            }

            if (career.Length < 2 || career.Length > AppConstants.MaximumCareerLength)
            {
                error = $"Escribe una carrera válida de hasta {AppConstants.MaximumCareerLength} caracteres.";
                return false;
            }

            if (campus.Length < 2 || campus.Length > AppConstants.MaximumCampusLength)
            {
                error = $"Escribe una sede válida de hasta {AppConstants.MaximumCampusLength} caracteres.";
                return false;
            }

            if (bio.Length > AppConstants.MaximumBioLength)
            {
                error = $"La presentación no puede superar {AppConstants.MaximumBioLength} caracteres.";
                return false;
            }

            if (skills.Count == 0 || skills.Count > AppConstants.MaximumSkills ||
                skills.Any(skill => skill.Length > AppConstants.MaximumSkillLength))
            {
                error = $"Agrega de 1 a {AppConstants.MaximumSkills} habilidades separadas por comas; " +
                        $"cada una puede tener hasta {AppConstants.MaximumSkillLength} caracteres.";
                return false;
            }

            if (portfolioUrl.Length > AppConstants.MaximumPortfolioUrlLength ||
                (portfolioUrl.Length > 0 &&
                 (!Uri.TryCreate(portfolioUrl, UriKind.Absolute, out Uri uri) ||
                  uri.Scheme != Uri.UriSchemeHttps)))
            {
                error = "El portafolio debe estar vacío o ser una dirección https:// válida.";
                return false;
            }

            profile = new UserProfile
            {
                Uid = uid,
                Email = AppConstants.NormalizeEmail(email),
                DisplayName = displayName,
                Career = career,
                Campus = campus,
                Bio = bio,
                Skills = skills,
                PortfolioUrl = portfolioUrl,
                AvatarId = avatarId
            };
            error = string.Empty;
            return true;
        }

        private void OnDisplayNameChanged(string value)
        {
            UpdateAvatarPreview();
        }

        private void UpdateAvatarPreview()
        {
            avatarPreview.color = AvatarColors[Mathf.Clamp(avatarId, 0, AvatarColors.Length - 1)];
            string name = (displayNameInput.text ?? string.Empty).Trim();
            avatarPreviewLabel.text = name.Length == 0
                ? (avatarId + 1).ToString()
                : name.Substring(0, 1).ToUpperInvariant();
            avatarIndexLabel.text = $"Avatar {avatarId + 1} de {AppConstants.AvailableAvatarCount}";
        }

        private void SetButtonsInteractable(bool interactable)
        {
            previousAvatarButton.interactable = interactable;
            nextAvatarButton.interactable = interactable;
            saveButton.interactable = interactable;
            cancelButton.interactable = interactable;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out Color color)
                ? color
                : Color.white;
        }

        private void OnDestroy()
        {
            previousAvatarButton.onClick.RemoveListener(OnPreviousAvatarPressed);
            nextAvatarButton.onClick.RemoveListener(OnNextAvatarPressed);
            saveButton.onClick.RemoveListener(OnSavePressed);
            cancelButton.onClick.RemoveListener(OnCancelPressed);
            displayNameInput.onValueChanged.RemoveListener(OnDisplayNameChanged);
        }
    }
}
