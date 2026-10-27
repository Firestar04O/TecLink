using System.Collections.Generic;
using System.Linq;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TecLink.EditorTools
{
    [InitializeOnLoad]
    public static class Phase2SceneBuilder
    {
        private const string ScenePath = "Assets/_TecLink/Scenes/TecLink.unity";

        private static readonly Color BackgroundColor = Hex("0D1B2A");
        private static readonly Color CardColor = Hex("15283D");
        private static readonly Color FieldColor = Hex("213A54");
        private static readonly Color PrimaryColor = Hex("16B8A6");
        private static readonly Color PrimaryPressedColor = Hex("0C8E81");
        private static readonly Color SecondaryColor = Hex("31516D");
        private static readonly Color TextColor = Hex("F5F8FC");
        private static readonly Color MutedTextColor = Hex("B9C7D5");
        private static readonly Color ErrorColor = Hex("FF6B6B");

        private static TMP_FontAsset defaultFont;

        static Phase2SceneBuilder()
        {
            EditorApplication.delayCall += BuildMissingSceneOnce;
        }

        [MenuItem("TecLink/Construir escena de Fase 6")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildSceneInternal(true);
        }

        public static void BuildSceneFromCommandLine()
        {
            BuildSceneInternal(false);
        }

        private static void BuildMissingSceneOnce()
        {
            if (Application.isBatchMode || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += BuildMissingSceneOnce;
                return;
            }

            BuildSceneInternal(false);
        }

        private static void BuildSceneInternal(bool leaveGeneratedSceneOpen)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                leaveGeneratedSceneOpen ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            CreateCamera();
            CreateEventSystem();

            var firebaseObject = new GameObject("App_Firebase");
            firebaseObject.AddComponent<FirebaseBootstrap>();

            var appRoot = new GameObject("App_Root");
            SessionManager sessionManager = appRoot.AddComponent<SessionManager>();
            AuthService authService = appRoot.AddComponent<AuthService>();
            FirestoreService firestoreService = appRoot.AddComponent<FirestoreService>();
            NavigationManager navigation = appRoot.AddComponent<NavigationManager>();
            AppManager appManager = appRoot.AddComponent<AppManager>();

            Canvas canvas = CreateCanvas();
            RectTransform canvasTransform = canvas.GetComponent<RectTransform>();
            RectTransform safeArea = CreateRect("SafeArea", canvasTransform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            GameObject loginScreen = BuildLoginScreen(safeArea);
            GameObject registerScreen = BuildRegisterScreen(safeArea);
            GameObject verifyScreen = BuildVerifyScreen(safeArea);
            GameObject profileScreen = BuildProfileScreen(safeArea);
            GameObject feedScreen = BuildFeedScreen(safeArea);
            GameObject createProjectScreen = BuildCreateProjectScreen(safeArea);
            GameObject projectDetailScreen = BuildProjectDetailScreen(safeArea);
            GameObject myProjectsScreen = BuildMyProjectsScreen(safeArea);
            GameObject applyProjectScreen = BuildApplyProjectScreen(safeArea);
            GameObject applicationsScreen = BuildApplicationsScreen(safeArea);
            GameObject chatsScreen = BuildChatsScreen(safeArea);
            GameObject chatScreen = BuildChatScreen(safeArea);

            LoadingPopup loadingPopup = BuildLoadingPopup(canvasTransform);
            ErrorPopup errorPopup = BuildErrorPopup(canvasTransform);
            ConfirmPopup confirmPopup = BuildConfirmPopup(canvasTransform);

            Assign(navigation, "loginScreen", loginScreen);
            Assign(navigation, "registerScreen", registerScreen);
            Assign(navigation, "verifyEmailScreen", verifyScreen);
            Assign(navigation, "profileSetupScreen", profileScreen);
            Assign(navigation, "feedScreen", feedScreen);
            Assign(navigation, "createProjectScreen", createProjectScreen);
            Assign(navigation, "projectDetailScreen", projectDetailScreen);
            Assign(navigation, "myProjectsScreen", myProjectsScreen);
            Assign(navigation, "applyProjectScreen", applyProjectScreen);
            Assign(navigation, "applicationsScreen", applicationsScreen);
            Assign(navigation, "chatsScreen", chatsScreen);
            Assign(navigation, "chatScreen", chatScreen);

            Assign(appManager, "navigation", navigation);
            Assign(appManager, "loadingPopup", loadingPopup);
            Assign(appManager, "errorPopup", errorPopup);
            Assign(appManager, "confirmPopup", confirmPopup);

            registerScreen.SetActive(false);
            verifyScreen.SetActive(false);
            profileScreen.SetActive(false);
            feedScreen.SetActive(false);
            createProjectScreen.SetActive(false);
            projectDetailScreen.SetActive(false);
            myProjectsScreen.SetActive(false);
            applyProjectScreen.SetActive(false);
            applicationsScreen.SetActive(false);
            chatsScreen.SetActive(false);
            chatScreen.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            PutSceneFirstInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (leaveGeneratedSceneOpen)
            {
                Selection.activeGameObject = appRoot;
            }
            else
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log($"[TecLink] Escena de Fase 6 construida y guardada en {ScenePath}.");
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas_Main", typeof(RectTransform));
            RectTransform canvasTransform = canvasObject.GetComponent<RectTransform>();
            canvasTransform.localScale = Vector3.one;
            canvasTransform.pivot = new Vector2(0.5f, 0.5f);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject BuildLoginScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_Login", parent);
            CreateBrandHeader(screen, "Conecta talentos.\nConstruye proyectos.");

            CreateCard(screen, new Vector2(0f, -95f), new Vector2(900f, 880f));
            CreateText(screen, "Title", "Inicia sesión", 52f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 315f), new Vector2(780f, 80f));
            CreateText(screen, "EmailLabel", "Correo institucional", 28f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(-5f, 215f), new Vector2(760f, 50f));
            TMP_InputField email = CreateInput(screen, "Input_Email", "nombre@tecsup.edu.pe",
                new Vector2(0f, 140f), TMP_InputField.ContentType.EmailAddress, AppConstants.MaximumEmailLength);

            CreateText(screen, "PasswordLabel", "Contraseña", 28f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(-5f, 25f), new Vector2(760f, 50f));
            TMP_InputField password = CreateInput(screen, "Input_Password", "Tu contraseña",
                new Vector2(0f, -50f), TMP_InputField.ContentType.Password, AppConstants.MaximumPasswordLength);

            Button loginButton = CreateButton(screen, "Button_Login", "Entrar",
                new Vector2(0f, -205f), new Vector2(760f, 104f), PrimaryColor);
            Button registerButton = CreateButton(screen, "Button_OpenRegister", "Crear una cuenta",
                new Vector2(0f, -340f), new Vector2(760f, 92f), SecondaryColor);

            CreateText(screen, "DomainHint", "Acceso exclusivo con correo @tecsup.edu.pe", 24f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, -700f), new Vector2(920f, 60f));

            LoginController controller = screen.gameObject.AddComponent<LoginController>();
            Assign(controller, "emailInput", email);
            Assign(controller, "passwordInput", password);
            Assign(controller, "loginButton", loginButton);
            Assign(controller, "openRegisterButton", registerButton);
            return screen.gameObject;
        }

        private static GameObject BuildRegisterScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_Register", parent);
            CreateText(screen, "Brand", "TecLink", 74f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 700f), new Vector2(900f, 110f));
            CreateText(screen, "Title", "Crea tu cuenta", 50f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 565f), new Vector2(850f, 80f));
            CreateText(screen, "Subtitle", "Usa únicamente tu correo institucional", 27f, FontStyles.Normal,
                TextAlignmentOptions.Center, MutedTextColor, new Vector2(0f, 500f), new Vector2(900f, 60f));

            CreateCard(screen, new Vector2(0f, -65f), new Vector2(900f, 1030f));
            CreateText(screen, "EmailLabel", "Correo institucional", 28f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(-5f, 335f), new Vector2(760f, 48f));
            TMP_InputField email = CreateInput(screen, "Input_Email", "nombre@tecsup.edu.pe",
                new Vector2(0f, 262f), TMP_InputField.ContentType.EmailAddress, AppConstants.MaximumEmailLength);

            CreateText(screen, "PasswordLabel", "Contraseña (mínimo 8 caracteres)", 28f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(-5f, 145f), new Vector2(760f, 48f));
            TMP_InputField password = CreateInput(screen, "Input_Password", "Crea una contraseña",
                new Vector2(0f, 72f), TMP_InputField.ContentType.Password, AppConstants.MaximumPasswordLength);

            CreateText(screen, "ConfirmLabel", "Confirma tu contraseña", 28f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(-5f, -45f), new Vector2(760f, 48f));
            TMP_InputField confirmation = CreateInput(screen, "Input_ConfirmPassword", "Repite la contraseña",
                new Vector2(0f, -118f), TMP_InputField.ContentType.Password, AppConstants.MaximumPasswordLength);

            Button registerButton = CreateButton(screen, "Button_Register", "Crear cuenta",
                new Vector2(0f, -285f), new Vector2(760f, 104f), PrimaryColor);
            Button backButton = CreateButton(screen, "Button_BackToLogin", "Volver a iniciar sesión",
                new Vector2(0f, -420f), new Vector2(760f, 92f), SecondaryColor);

            RegisterController controller = screen.gameObject.AddComponent<RegisterController>();
            Assign(controller, "emailInput", email);
            Assign(controller, "passwordInput", password);
            Assign(controller, "confirmPasswordInput", confirmation);
            Assign(controller, "registerButton", registerButton);
            Assign(controller, "backToLoginButton", backButton);
            return screen.gameObject;
        }

        private static GameObject BuildVerifyScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_VerifyEmail", parent);
            CreateText(screen, "Icon", "@", 130f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 560f), new Vector2(220f, 180f));
            CreateText(screen, "Title", "Verifica tu correo", 55f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 415f), new Vector2(900f, 90f));
            CreateText(screen, "Description",
                "Enviamos un enlace de verificación a:", 29f, FontStyles.Normal,
                TextAlignmentOptions.Center, MutedTextColor, new Vector2(0f, 320f), new Vector2(860f, 80f));
            TMP_Text emailLabel = CreateText(screen, "Email", "correo@tecsup.edu.pe", 32f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 240f), new Vector2(880f, 70f));
            TMP_Text statusLabel = CreateText(screen, "Status",
                "Abre el enlace que Firebase envió a tu correo. Revisa también Spam.",
                27f, FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, 105f), new Vector2(820f, 150f));

            Button verifiedButton = CreateButton(screen, "Button_AlreadyVerified", "Ya verifiqué mi correo",
                new Vector2(0f, -80f), new Vector2(820f, 110f), PrimaryColor);
            Button resendButton = CreateButton(screen, "Button_Resend", "Reenviar correo",
                new Vector2(0f, -225f), new Vector2(820f, 96f), SecondaryColor);
            Button logoutButton = CreateButton(screen, "Button_Logout", "Cerrar sesión",
                new Vector2(0f, -370f), new Vector2(820f, 92f), CardColor);

            CreateText(screen, "Hint",
                "Después de abrir el enlace, regresa a TecLink y pulsa “Ya verifiqué mi correo”.",
                25f, FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, -560f), new Vector2(850f, 140f));

            VerifyEmailController controller = screen.gameObject.AddComponent<VerifyEmailController>();
            Assign(controller, "emailLabel", emailLabel);
            Assign(controller, "statusLabel", statusLabel);
            Assign(controller, "resendButton", resendButton);
            Assign(controller, "alreadyVerifiedButton", verifiedButton);
            Assign(controller, "logoutButton", logoutButton);
            return screen.gameObject;
        }

        private static GameObject BuildProfileScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_ProfileSetup", parent);
            CreateCard(screen, new Vector2(0f, -5f), new Vector2(940f, 1640f));
            CreateText(screen, "Brand", "TecLink", 50f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 820f), new Vector2(900f, 70f));
            TMP_Text titleLabel = CreateText(screen, "Title", "Completa tu perfil", 42f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 750f), new Vector2(900f, 65f));
            TMP_Text emailLabel = CreateText(screen, "Email", "correo@tecsup.edu.pe", 31f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 688f), new Vector2(900f, 55f));

            RectTransform avatarRoot = CreateRect("AvatarPreview", screen);
            SetRect(avatarRoot, new Vector2(-260f, 582f), new Vector2(110f, 110f));
            Image avatarImage = avatarRoot.gameObject.AddComponent<Image>();
            avatarImage.color = PrimaryColor;
            TMP_Text avatarPreviewLabel = CreateText(avatarRoot, "Initial", "1", 48f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, Vector2.zero, Vector2.zero);
            Stretch(avatarPreviewLabel.rectTransform);

            Button previousAvatarButton = CreateButton(screen, "Button_PreviousAvatar", "<",
                new Vector2(-105f, 582f), new Vector2(90f, 82f), SecondaryColor);
            TMP_Text avatarIndexLabel = CreateText(screen, "AvatarIndex", "Avatar 1 de 6", 26f,
                FontStyles.Normal, TextAlignmentOptions.Center, TextColor,
                new Vector2(100f, 582f), new Vector2(260f, 70f));
            Button nextAvatarButton = CreateButton(screen, "Button_NextAvatar", ">",
                new Vector2(310f, 582f), new Vector2(90f, 82f), SecondaryColor);

            CreateText(screen, "DisplayNameLabel", "Nombre para mostrar", 23f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 495f), new Vector2(800f, 36f));
            TMP_InputField displayName = CreateInput(screen, "Input_DisplayName", "Tu nombre",
                new Vector2(0f, 446f), new Vector2(800f, 82f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumDisplayNameLength);

            CreateText(screen, "CareerLabel", "Carrera", 23f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 375f), new Vector2(800f, 36f));
            TMP_InputField career = CreateInput(screen, "Input_Career", "Ej. Diseño y Desarrollo de Software",
                new Vector2(0f, 326f), new Vector2(800f, 82f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumCareerLength);

            CreateText(screen, "CampusLabel", "Sede", 23f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 255f), new Vector2(800f, 36f));
            TMP_InputField campus = CreateInput(screen, "Input_Campus", "Ej. Lima Centro",
                new Vector2(0f, 206f), new Vector2(800f, 82f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumCampusLength);

            CreateText(screen, "BioLabel", "Presentación breve (opcional)", 23f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 135f), new Vector2(800f, 36f));
            TMP_InputField bio = CreateInput(screen, "Input_Bio", "Cuéntanos qué te interesa construir",
                new Vector2(0f, 66f), new Vector2(800f, 120f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumBioLength, true);

            CreateText(screen, "SkillsLabel", "Habilidades (separadas por comas)", 23f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, -25f), new Vector2(800f, 36f));
            TMP_InputField skills = CreateInput(screen, "Input_Skills", "Unity, C#, Diseño 3D",
                new Vector2(0f, -74f), new Vector2(800f, 82f), TMP_InputField.ContentType.Standard,
                500);

            CreateText(screen, "PortfolioLabel", "Portafolio https:// (opcional)", 23f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, -145f), new Vector2(800f, 36f));
            TMP_InputField portfolio = CreateInput(screen, "Input_Portfolio", "https://...",
                new Vector2(0f, -194f), new Vector2(800f, 82f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumPortfolioUrlLength);

            Button saveButton = CreateButton(screen, "Button_SaveProfile", "Guardar perfil",
                new Vector2(0f, -315f), new Vector2(800f, 94f), PrimaryColor);
            Button cancelButton = CreateButton(screen, "Button_CancelEdit", "Cancelar edición",
                new Vector2(0f, -425f), new Vector2(800f, 86f), SecondaryColor);
            CreateText(screen, "Hint",
                "Estos datos se guardarán en Firestore. Podrás editarlos después.", 22f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, -535f), new Vector2(820f, 70f));

            ProfileController controller = screen.gameObject.AddComponent<ProfileController>();
            Assign(controller, "titleLabel", titleLabel);
            Assign(controller, "emailLabel", emailLabel);
            Assign(controller, "avatarPreview", avatarImage);
            Assign(controller, "avatarPreviewLabel", avatarPreviewLabel);
            Assign(controller, "avatarIndexLabel", avatarIndexLabel);
            Assign(controller, "displayNameInput", displayName);
            Assign(controller, "careerInput", career);
            Assign(controller, "campusInput", campus);
            Assign(controller, "bioInput", bio);
            Assign(controller, "skillsInput", skills);
            Assign(controller, "portfolioUrlInput", portfolio);
            Assign(controller, "previousAvatarButton", previousAvatarButton);
            Assign(controller, "nextAvatarButton", nextAvatarButton);
            Assign(controller, "saveButton", saveButton);
            Assign(controller, "cancelButton", cancelButton);
            return screen.gameObject;
        }

        private static GameObject BuildFeedScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_Feed", parent);
            CreateText(screen, "Brand", "TecLink", 60f, FontStyles.Bold,
                TextAlignmentOptions.Left, PrimaryColor, new Vector2(-260f, 820f), new Vector2(380f, 80f));
            TMP_Text greeting = CreateText(screen, "Greeting", "Proyectos abiertos", 32f,
                FontStyles.Bold, TextAlignmentOptions.Left, TextColor,
                new Vector2(-185f, 750f), new Vector2(530f, 55f));
            Button refreshButton = CreateButton(screen, "Button_RefreshFeed", "Actualizar",
                new Vector2(320f, 758f), new Vector2(220f, 66f), SecondaryColor);
            TMP_InputField filter = CreateInput(screen, "Input_FeedFilter",
                "Filtrar por habilidad, carrera, categoría o sede",
                new Vector2(0f, 665f), new Vector2(900f, 82f), TMP_InputField.ContentType.Standard, 80);

            RectTransform content = CreateProjectList(screen, "FeedProjectList",
                new Vector2(0f, 75f), new Vector2(920f, 1090f),
                out ProjectCardView template, out TMP_Text emptyLabel);

            Button createProjectButton = CreateButton(screen, "Button_CreateProject", "Publicar",
                new Vector2(-290f, -735f), new Vector2(260f, 76f), PrimaryColor);
            Button myProjectsButton = CreateButton(screen, "Button_MyProjects", "Mis proyectos",
                new Vector2(0f, -735f), new Vector2(260f, 76f), SecondaryColor);
            Button chatsButton = CreateButton(screen, "Button_Chats", "Chats",
                new Vector2(290f, -735f), new Vector2(260f, 76f), SecondaryColor);
            Button editButton = CreateButton(screen, "Button_EditProfile", "Editar mi perfil",
                new Vector2(-220f, -825f), new Vector2(400f, 72f), SecondaryColor);
            Button logoutButton = CreateButton(screen, "Button_Logout", "Cerrar sesión",
                new Vector2(220f, -825f), new Vector2(400f, 72f), SecondaryColor);

            FeedController controller = screen.gameObject.AddComponent<FeedController>();
            Assign(controller, "greetingLabel", greeting);
            Assign(controller, "filterInput", filter);
            Assign(controller, "emptyLabel", emptyLabel);
            Assign(controller, "cardsContent", content);
            Assign(controller, "cardTemplate", template);
            Assign(controller, "refreshButton", refreshButton);
            Assign(controller, "createProjectButton", createProjectButton);
            Assign(controller, "myProjectsButton", myProjectsButton);
            Assign(controller, "chatsButton", chatsButton);
            Assign(controller, "editProfileButton", editButton);
            Assign(controller, "logoutButton", logoutButton);
            return screen.gameObject;
        }

        private static GameObject BuildCreateProjectScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_CreateProject", parent);
            TMP_Text screenTitle = CreateText(screen, "Title", "Publica un proyecto", 52f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 815f), new Vector2(920f, 80f));
            TMP_Text screenSubtitle = CreateText(screen, "Subtitle", "Encuentra estudiantes con las habilidades que necesitas",
                24f, FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, 750f), new Vector2(900f, 60f));
            CreateCard(screen, new Vector2(0f, -5f), new Vector2(940f, 1460f));

            CreateText(screen, "TitleLabel", "Título", 22f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 660f), new Vector2(820f, 30f));
            TMP_InputField title = CreateInput(screen, "Input_ProjectTitle", "Ej. Busco modelador 3D",
                new Vector2(0f, 615f), new Vector2(820f, 72f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumProjectTitleLength);

            CreateText(screen, "DescriptionLabel", "Descripción", 22f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 550f), new Vector2(820f, 30f));
            TMP_InputField description = CreateInput(screen, "Input_ProjectDescription",
                "Explica el objetivo, el avance actual y qué colaboración buscas",
                new Vector2(0f, 475f), new Vector2(820f, 125f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumProjectDescriptionLength, true);

            CreateText(screen, "CategoryLabel", "Categoría", 22f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 380f), new Vector2(820f, 30f));
            TMP_InputField category = CreateInput(screen, "Input_ProjectCategory", "Ej. Videojuegos",
                new Vector2(0f, 335f), new Vector2(820f, 72f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumProjectCategoryLength);

            CreateText(screen, "SkillsLabel", "Habilidades requeridas (separadas por comas)", 22f,
                FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(0f, 270f), new Vector2(820f, 30f));
            TMP_InputField skills = CreateInput(screen, "Input_RequiredSkills", "Modelado 3D, Blender",
                new Vector2(0f, 225f), new Vector2(820f, 72f), TMP_InputField.ContentType.Standard, 500);

            CreateText(screen, "ToolsLabel", "Herramientas (opcional, separadas por comas)", 22f,
                FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(0f, 160f), new Vector2(820f, 30f));
            TMP_InputField tools = CreateInput(screen, "Input_ProjectTools", "Unity, Git, Blender",
                new Vector2(0f, 115f), new Vector2(820f, 72f), TMP_InputField.ContentType.Standard, 500);

            CreateText(screen, "ModalityLabel", "Modalidad (toca para cambiar)", 22f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, 50f), new Vector2(820f, 30f));
            Button modalityButton = CreateButton(screen, "Button_Modality", "Presencial",
                new Vector2(0f, 5f), new Vector2(820f, 72f), FieldColor);
            TMP_Text modalityText = modalityButton.GetComponentInChildren<TMP_Text>();

            CreateText(screen, "CampusLabel", "Sede", 22f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, -60f), new Vector2(820f, 30f));
            TMP_InputField campus = CreateInput(screen, "Input_ProjectCampus", "Ej. Lima Centro",
                new Vector2(0f, -105f), new Vector2(820f, 72f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumCampusLength);

            CreateText(screen, "HoursLabel", "Horas aproximadas por semana", 22f, FontStyles.Normal,
                TextAlignmentOptions.Left, MutedTextColor, new Vector2(0f, -170f), new Vector2(820f, 30f));
            TMP_InputField hours = CreateInput(screen, "Input_HoursPerWeek", "Ej. 6",
                new Vector2(0f, -215f), new Vector2(820f, 72f), TMP_InputField.ContentType.IntegerNumber, 2);

            CreateText(screen, "DeadlineLabel", "Fecha límite opcional (AAAA-MM-DD)", 22f,
                FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(0f, -280f), new Vector2(820f, 30f));
            TMP_InputField deadline = CreateInput(screen, "Input_ProjectDeadline", "Ej. 2026-12-20",
                new Vector2(0f, -325f), new Vector2(820f, 72f), TMP_InputField.ContentType.Standard, 10);

            Button createButton = CreateButton(screen, "Button_PublishProject", "Publicar proyecto",
                new Vector2(0f, -470f), new Vector2(820f, 92f), PrimaryColor);
            TMP_Text submitLabel = createButton.GetComponentInChildren<TMP_Text>();
            Button cancelButton = CreateButton(screen, "Button_CancelProject", "Cancelar",
                new Vector2(0f, -580f), new Vector2(820f, 84f), SecondaryColor);

            CreateProjectController controller = screen.gameObject.AddComponent<CreateProjectController>();
            Assign(controller, "screenTitleLabel", screenTitle);
            Assign(controller, "screenSubtitleLabel", screenSubtitle);
            Assign(controller, "submitButtonLabel", submitLabel);
            Assign(controller, "titleInput", title);
            Assign(controller, "descriptionInput", description);
            Assign(controller, "categoryInput", category);
            Assign(controller, "requiredSkillsInput", skills);
            Assign(controller, "toolsInput", tools);
            Assign(controller, "modalityButton", modalityButton);
            Assign(controller, "modalityLabel", modalityText);
            Assign(controller, "campusInput", campus);
            Assign(controller, "hoursPerWeekInput", hours);
            Assign(controller, "deadlineInput", deadline);
            Assign(controller, "createButton", createButton);
            Assign(controller, "cancelButton", cancelButton);
            return screen.gameObject;
        }

        private static GameObject BuildProjectDetailScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_ProjectDetail", parent);
            CreateText(screen, "Header", "Detalle del proyecto", 48f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 820f), new Vector2(720f, 75f));

            RectTransform content = CreateScrollContent(screen, "ProjectDetailScroll",
                new Vector2(0f, 80f), new Vector2(920f, 1360f), 1580f);
            CreateCard(content, new Vector2(0f, 0f), new Vector2(880f, 1520f));
            TMP_Text title = CreateText(content, "ProjectTitle", "Título", 45f, FontStyles.Bold,
                TextAlignmentOptions.Left, TextColor, new Vector2(0f, 650f), new Vector2(790f, 110f));
            TMP_Text status = CreateText(content, "ProjectStatus", "Abierto", 27f, FontStyles.Bold,
                TextAlignmentOptions.Left, PrimaryColor, new Vector2(0f, 570f), new Vector2(790f, 45f));
            TMP_Text owner = CreateText(content, "ProjectOwner", "Publicado por", 27f,
                FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(0f, 515f), new Vector2(790f, 50f));
            TMP_Text metadata = CreateText(content, "ProjectMetadata", "Categoría · Modalidad · Sede",
                27f, FontStyles.Normal, TextAlignmentOptions.Left, TextColor,
                new Vector2(0f, 450f), new Vector2(790f, 70f));
            CreateText(content, "DescriptionHeader", "Descripción", 28f, FontStyles.Bold,
                TextAlignmentOptions.Left, PrimaryColor, new Vector2(0f, 370f), new Vector2(790f, 45f));
            TMP_Text description = CreateText(content, "ProjectDescription", "Descripción del proyecto",
                27f, FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor,
                new Vector2(0f, 160f), new Vector2(790f, 360f));
            TMP_Text skills = CreateText(content, "ProjectSkills", "Habilidades requeridas",
                27f, FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor,
                new Vector2(0f, -90f), new Vector2(790f, 110f));
            TMP_Text tools = CreateText(content, "ProjectTools", "Herramientas",
                27f, FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor,
                new Vector2(0f, -205f), new Vector2(790f, 90f));
            TMP_Text deadline = CreateText(content, "ProjectDeadline", "Fecha límite",
                27f, FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(0f, -295f), new Vector2(790f, 55f));
            TMP_Text actionHint = CreateText(content, "ActionHint", "Acciones disponibles",
                26f, FontStyles.Italic, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, -425f), new Vector2(790f, 120f));

            Button editButton = CreateButton(screen, "Button_EditProject", "Editar",
                new Vector2(-330f, -790f), new Vector2(205f, 86f), PrimaryColor);
            Button closeButton = CreateButton(screen, "Button_CloseProject", "Cerrar proyecto",
                new Vector2(-110f, -790f), new Vector2(205f, 86f), ErrorColor);
            Button interestButton = CreateButton(screen, "Button_Interest", "Me interesa",
                new Vector2(-110f, -790f), new Vector2(205f, 86f), PrimaryColor);
            Button applicationsButton = CreateButton(screen, "Button_Applications", "Postulantes",
                new Vector2(110f, -790f), new Vector2(205f, 86f), SecondaryColor);
            Button backButton = CreateButton(screen, "Button_BackFromDetail", "Volver",
                new Vector2(330f, -790f), new Vector2(205f, 86f), SecondaryColor);

            ProjectDetailController controller = screen.gameObject.AddComponent<ProjectDetailController>();
            Assign(controller, "titleLabel", title);
            Assign(controller, "statusLabel", status);
            Assign(controller, "ownerLabel", owner);
            Assign(controller, "metadataLabel", metadata);
            Assign(controller, "descriptionLabel", description);
            Assign(controller, "skillsLabel", skills);
            Assign(controller, "toolsLabel", tools);
            Assign(controller, "deadlineLabel", deadline);
            Assign(controller, "actionHintLabel", actionHint);
            Assign(controller, "editButton", editButton);
            Assign(controller, "closeProjectButton", closeButton);
            Assign(controller, "interestButton", interestButton);
            Assign(controller, "applicationsButton", applicationsButton);
            Assign(controller, "backButton", backButton);
            return screen.gameObject;
        }

        private static GameObject BuildMyProjectsScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_MyProjects", parent);
            CreateText(screen, "Title", "Mis proyectos", 54f, FontStyles.Bold,
                TextAlignmentOptions.Left, TextColor, new Vector2(-170f, 815f), new Vector2(560f, 80f));
            Button refreshButton = CreateButton(screen, "Button_RefreshMyProjects", "Actualizar",
                new Vector2(320f, 815f), new Vector2(220f, 66f), SecondaryColor);

            RectTransform content = CreateProjectList(screen, "MyProjectList",
                new Vector2(0f, 55f), new Vector2(920f, 1370f),
                out ProjectCardView template, out TMP_Text emptyLabel);
            emptyLabel.text = "Todavía no has publicado proyectos.";

            Button createButton = CreateButton(screen, "Button_CreateFromMyProjects", "Crear proyecto",
                new Vector2(-225f, -790f), new Vector2(400f, 86f), PrimaryColor);
            Button backButton = CreateButton(screen, "Button_BackFromMyProjects", "Volver al feed",
                new Vector2(225f, -790f), new Vector2(400f, 86f), SecondaryColor);

            MyProjectsController controller = screen.gameObject.AddComponent<MyProjectsController>();
            Assign(controller, "emptyLabel", emptyLabel);
            Assign(controller, "cardsContent", content);
            Assign(controller, "cardTemplate", template);
            Assign(controller, "refreshButton", refreshButton);
            Assign(controller, "createButton", createButton);
            Assign(controller, "backButton", backButton);
            return screen.gameObject;
        }

        private static GameObject BuildApplyProjectScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_ApplyProject", parent);
            CreateText(screen, "Title", "Me interesa", 58f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 700f), new Vector2(900f, 90f));
            TMP_Text projectTitle = CreateText(screen, "ProjectTitle", "Título del proyecto", 38f,
                FontStyles.Bold, TextAlignmentOptions.Center, TextColor,
                new Vector2(0f, 590f), new Vector2(860f, 110f));
            CreateCard(screen, new Vector2(0f, 45f), new Vector2(900f, 900f));
            CreateText(screen, "Instruction",
                "Preséntate brevemente y explica cómo podrías aportar al proyecto.", 29f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, 350f), new Vector2(790f, 120f));
            TMP_InputField message = CreateInput(screen, "Input_InterestMessage",
                "Ej. Tengo experiencia con Blender y puedo apoyar con los modelos...",
                new Vector2(0f, 70f), new Vector2(790f, 360f), TMP_InputField.ContentType.Standard,
                AppConstants.MaximumInterestMessageLength, true);
            TMP_Text characterCount = CreateText(screen, "CharacterCount", "0/500", 22f,
                FontStyles.Normal, TextAlignmentOptions.Right, MutedTextColor,
                new Vector2(0f, -145f), new Vector2(790f, 40f));
            Button submitButton = CreateButton(screen, "Button_SubmitInterest", "Enviar postulación",
                new Vector2(0f, -300f), new Vector2(790f, 96f), PrimaryColor);
            Button cancelButton = CreateButton(screen, "Button_CancelInterest", "Cancelar",
                new Vector2(0f, -420f), new Vector2(790f, 86f), SecondaryColor);
            CreateText(screen, "Hint", "Solo puedes postular una vez a cada proyecto.", 23f,
                FontStyles.Italic, TextAlignmentOptions.Center, MutedTextColor,
                new Vector2(0f, -535f), new Vector2(790f, 60f));

            ApplyProjectController controller = screen.gameObject.AddComponent<ApplyProjectController>();
            Assign(controller, "projectTitleLabel", projectTitle);
            Assign(controller, "messageInput", message);
            Assign(controller, "characterCountLabel", characterCount);
            Assign(controller, "submitButton", submitButton);
            Assign(controller, "cancelButton", cancelButton);
            return screen.gameObject;
        }

        private static GameObject BuildApplicationsScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_Applications", parent);
            CreateText(screen, "Title", "Postulantes", 54f, FontStyles.Bold,
                TextAlignmentOptions.Left, TextColor, new Vector2(-185f, 825f), new Vector2(530f, 75f));
            Button refreshButton = CreateButton(screen, "Button_RefreshApplications", "Actualizar",
                new Vector2(320f, 825f), new Vector2(220f, 66f), SecondaryColor);
            TMP_Text projectTitle = CreateText(screen, "ProjectTitle", "Proyecto", 29f,
                FontStyles.Bold, TextAlignmentOptions.Left, PrimaryColor,
                new Vector2(0f, 745f), new Vector2(900f, 60f));

            RectTransform content = CreateInterestList(screen, "ApplicationsList",
                new Vector2(0f, 25f), new Vector2(920f, 1350f),
                out InterestCardView template, out TMP_Text emptyLabel);
            Button backButton = CreateButton(screen, "Button_BackFromApplications", "Volver al proyecto",
                new Vector2(0f, -800f), new Vector2(820f, 86f), SecondaryColor);

            ApplicationsController controller = screen.gameObject.AddComponent<ApplicationsController>();
            Assign(controller, "projectTitleLabel", projectTitle);
            Assign(controller, "emptyLabel", emptyLabel);
            Assign(controller, "cardsContent", content);
            Assign(controller, "cardTemplate", template);
            Assign(controller, "refreshButton", refreshButton);
            Assign(controller, "backButton", backButton);
            return screen.gameObject;
        }

        private static GameObject BuildChatsScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_Chats", parent);
            CreateText(screen, "Title", "Chats", 56f, FontStyles.Bold,
                TextAlignmentOptions.Left, TextColor,
                new Vector2(-210f, 825f), new Vector2(480f, 80f));
            Button refreshButton = CreateButton(screen, "Button_RefreshChats", "Actualizar",
                new Vector2(320f, 825f), new Vector2(220f, 66f), SecondaryColor);

            RectTransform content = CreateChatList(screen, "ChatList",
                new Vector2(0f, 55f), new Vector2(920f, 1370f),
                out ChatCardView template, out TMP_Text emptyLabel);
            Button backButton = CreateButton(screen, "Button_BackFromChats", "Volver al feed",
                new Vector2(0f, -800f), new Vector2(820f, 86f), SecondaryColor);

            ChatListController controller = screen.gameObject.AddComponent<ChatListController>();
            Assign(controller, "emptyLabel", emptyLabel);
            Assign(controller, "cardsContent", content);
            Assign(controller, "cardTemplate", template);
            Assign(controller, "refreshButton", refreshButton);
            Assign(controller, "backButton", backButton);
            return screen.gameObject;
        }

        private static GameObject BuildChatScreen(RectTransform parent)
        {
            RectTransform screen = CreateScreen("Screen_Chat", parent);
            Button backButton = CreateButton(screen, "Button_BackFromChat", "Atrás",
                new Vector2(-380f, 830f), new Vector2(170f, 68f), SecondaryColor);
            TMP_Text projectTitle = CreateText(screen, "ProjectTitle", "Conversación", 38f,
                FontStyles.Bold, TextAlignmentOptions.Center, TextColor,
                new Vector2(80f, 830f), new Vector2(700f, 70f));
            TMP_Text role = CreateText(screen, "Role", "Conversación privada", 23f,
                FontStyles.Normal, TextAlignmentOptions.Center, PrimaryColor,
                new Vector2(0f, 765f), new Vector2(880f, 45f));

            RectTransform messagesContent = CreateChatMessageList(screen, "MessageList",
                new Vector2(0f, 55f), new Vector2(920f, 1290f),
                out ChatMessageView messageTemplate, out TMP_Text emptyLabel,
                out ScrollRect messagesScroll);

            TMP_InputField messageInput = CreateInput(screen, "Input_ChatMessage",
                "Escribe un mensaje...", new Vector2(-85f, -690f), new Vector2(710f, 125f),
                TMP_InputField.ContentType.Standard, AppConstants.MaximumChatMessageLength, true);
            Button sendButton = CreateButton(screen, "Button_SendMessage", "Enviar",
                new Vector2(375f, -690f), new Vector2(170f, 125f), PrimaryColor);
            TMP_Text characterCount = CreateText(screen, "CharacterCount", "0/1000", 21f,
                FontStyles.Normal, TextAlignmentOptions.Right, MutedTextColor,
                new Vector2(0f, -780f), new Vector2(880f, 38f));

            ChatController controller = screen.gameObject.AddComponent<ChatController>();
            Assign(controller, "projectTitleLabel", projectTitle);
            Assign(controller, "roleLabel", role);
            Assign(controller, "emptyLabel", emptyLabel);
            Assign(controller, "messagesContent", messagesContent);
            Assign(controller, "messageTemplate", messageTemplate);
            Assign(controller, "messagesScroll", messagesScroll);
            Assign(controller, "messageInput", messageInput);
            Assign(controller, "characterCountLabel", characterCount);
            Assign(controller, "sendButton", sendButton);
            Assign(controller, "backButton", backButton);
            return screen.gameObject;
        }

        private static LoadingPopup BuildLoadingPopup(RectTransform parent)
        {
            RectTransform root = CreateRect("Popup_Loading", parent);
            Stretch(root);
            Image overlay = root.gameObject.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.05f, 0.09f, 0.82f);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = CreateRect("Card", root);
            SetRect(card, new Vector2(0f, 0f), new Vector2(760f, 310f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            CreateText(card, "Activity", "●  ●  ●", 42f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 60f), new Vector2(600f, 80f));
            TMP_Text message = CreateText(card, "Message", "Procesando...", 31f, FontStyles.Normal,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, -45f), new Vector2(650f, 90f));

            LoadingPopup popup = root.gameObject.AddComponent<LoadingPopup>();
            Assign(popup, "canvasGroup", group);
            Assign(popup, "messageLabel", message);
            return popup;
        }

        private static ErrorPopup BuildErrorPopup(RectTransform parent)
        {
            RectTransform root = CreateRect("Popup_Error", parent);
            Stretch(root);
            Image overlay = root.gameObject.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.05f, 0.09f, 0.86f);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = CreateRect("Card", root);
            SetRect(card, new Vector2(0f, 0f), new Vector2(860f, 530f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            CreateText(card, "Title", "No se pudo completar", 40f, FontStyles.Bold,
                TextAlignmentOptions.Center, ErrorColor, new Vector2(0f, 150f), new Vector2(750f, 70f));
            TMP_Text message = CreateText(card, "Message", "Mensaje de error", 29f, FontStyles.Normal,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 25f), new Vector2(730f, 170f));
            Button closeButton = CreateButton(card, "Button_Close", "Entendido",
                new Vector2(0f, -155f), new Vector2(650f, 94f), PrimaryColor);

            ErrorPopup popup = root.gameObject.AddComponent<ErrorPopup>();
            Assign(popup, "canvasGroup", group);
            Assign(popup, "messageLabel", message);
            Assign(popup, "closeButton", closeButton);
            return popup;
        }

        private static ConfirmPopup BuildConfirmPopup(RectTransform parent)
        {
            RectTransform root = CreateRect("Popup_Confirm", parent);
            Stretch(root);
            Image overlay = root.gameObject.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.05f, 0.09f, 0.86f);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = CreateRect("Card", root);
            SetRect(card, new Vector2(0f, 0f), new Vector2(880f, 560f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            CreateText(card, "Title", "Confirma la acción", 40f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 175f), new Vector2(760f, 70f));
            TMP_Text message = CreateText(card, "Message", "¿Deseas continuar?", 28f,
                FontStyles.Normal, TextAlignmentOptions.Center, TextColor,
                new Vector2(0f, 45f), new Vector2(730f, 170f));
            Button confirmButton = CreateButton(card, "Button_Confirm", "Confirmar",
                new Vector2(-190f, -175f), new Vector2(330f, 90f), ErrorColor);
            Button cancelButton = CreateButton(card, "Button_Cancel", "Cancelar",
                new Vector2(190f, -175f), new Vector2(330f, 90f), SecondaryColor);

            ConfirmPopup popup = root.gameObject.AddComponent<ConfirmPopup>();
            Assign(popup, "canvasGroup", group);
            Assign(popup, "messageLabel", message);
            Assign(popup, "confirmButton", confirmButton);
            Assign(popup, "cancelButton", cancelButton);
            return popup;
        }

        private static RectTransform CreateScreen(string name, RectTransform parent)
        {
            RectTransform screen = CreateRect(name, parent);
            Stretch(screen);
            Image background = screen.gameObject.AddComponent<Image>();
            background.color = BackgroundColor;
            return screen;
        }

        private static void CreateBrandHeader(RectTransform parent, string subtitle)
        {
            CreateText(parent, "Brand", "TecLink", 84f, FontStyles.Bold,
                TextAlignmentOptions.Center, PrimaryColor, new Vector2(0f, 710f), new Vector2(920f, 120f));
            CreateText(parent, "Tagline", subtitle, 32f, FontStyles.Normal,
                TextAlignmentOptions.Center, TextColor, new Vector2(0f, 580f), new Vector2(900f, 130f));
        }

        private static Image CreateCard(RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform card = CreateRect("Card", parent);
            SetRect(card, position, size);
            Image image = card.gameObject.AddComponent<Image>();
            image.color = CardColor;
            image.raycastTarget = false;
            card.SetAsFirstSibling();
            return image;
        }

        private static RectTransform CreateProjectList(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            out ProjectCardView template,
            out TMP_Text emptyLabel)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(CardColor.r, CardColor.g, CardColor.b, 0.35f);

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 45f;

            template = CreateProjectCardTemplate(content);
            template.gameObject.SetActive(false);
            emptyLabel = CreateText(root, "EmptyState", "No hay proyectos para mostrar.", 29f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                Vector2.zero, new Vector2(size.x - 100f, 160f));
            return content;
        }

        private static ProjectCardView CreateProjectCardTemplate(RectTransform parent)
        {
            RectTransform card = CreateRect("ProjectCardTemplate", parent);
            card.sizeDelta = new Vector2(0f, 280f);
            Image image = card.gameObject.AddComponent<Image>();
            image.color = CardColor;
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 280f;
            layout.flexibleWidth = 1f;

            TMP_Text title = CreateText(card, "Title", "Título del proyecto", 32f, FontStyles.Bold,
                TextAlignmentOptions.Left, TextColor, new Vector2(0f, 95f), new Vector2(790f, 55f));
            TMP_Text owner = CreateText(card, "Owner", "Por estudiante · Carrera", 22f,
                FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(0f, 50f), new Vector2(790f, 38f));
            TMP_Text metadata = CreateText(card, "Metadata", "Modalidad · Sede · horas", 23f,
                FontStyles.Normal, TextAlignmentOptions.Left, TextColor,
                new Vector2(0f, 8f), new Vector2(790f, 38f));
            TMP_Text skills = CreateText(card, "Skills", "Busca: habilidades", 22f,
                FontStyles.Normal, TextAlignmentOptions.Left, TextColor,
                new Vector2(-90f, -45f), new Vector2(610f, 60f));
            TMP_Text status = CreateText(card, "Status", "Abierto", 22f, FontStyles.Bold,
                TextAlignmentOptions.Left, PrimaryColor, new Vector2(-280f, -105f), new Vector2(260f, 40f));
            Button openButton = CreateButton(card, "Button_OpenProject", "Ver detalle",
                new Vector2(280f, -102f), new Vector2(230f, 62f), SecondaryColor);

            ProjectCardView view = card.gameObject.AddComponent<ProjectCardView>();
            Assign(view, "titleLabel", title);
            Assign(view, "ownerLabel", owner);
            Assign(view, "metadataLabel", metadata);
            Assign(view, "skillsLabel", skills);
            Assign(view, "statusLabel", status);
            Assign(view, "openButton", openButton);
            return view;
        }

        private static RectTransform CreateInterestList(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            out InterestCardView template,
            out TMP_Text emptyLabel)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(CardColor.r, CardColor.g, CardColor.b, 0.35f);

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 45f;

            template = CreateInterestCardTemplate(content);
            template.gameObject.SetActive(false);
            emptyLabel = CreateText(root, "EmptyState", "Aún no hay postulaciones.", 29f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                Vector2.zero, new Vector2(size.x - 100f, 160f));
            return content;
        }

        private static InterestCardView CreateInterestCardTemplate(RectTransform parent)
        {
            RectTransform card = CreateRect("InterestCardTemplate", parent);
            card.sizeDelta = new Vector2(0f, 350f);
            Image image = card.gameObject.AddComponent<Image>();
            image.color = CardColor;
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 350f;
            layout.flexibleWidth = 1f;

            TMP_Text applicantName = CreateText(card, "ApplicantName", "Nombre del postulante", 31f,
                FontStyles.Bold, TextAlignmentOptions.Left, TextColor,
                new Vector2(0f, 135f), new Vector2(790f, 50f));
            TMP_Text message = CreateText(card, "Message", "Mensaje de postulación", 24f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor,
                new Vector2(0f, 45f), new Vector2(790f, 125f));
            TMP_Text status = CreateText(card, "Status", "Pendiente", 22f,
                FontStyles.Bold, TextAlignmentOptions.Left, PrimaryColor,
                new Vector2(-255f, -45f), new Vector2(280f, 42f));
            Button rejectButton = CreateButton(card, "Button_RejectInterest", "Rechazar",
                new Vector2(-205f, -120f), new Vector2(350f, 68f), SecondaryColor);
            Button acceptButton = CreateButton(card, "Button_AcceptInterest", "Aceptar",
                new Vector2(205f, -120f), new Vector2(350f, 68f), PrimaryColor);

            InterestCardView view = card.gameObject.AddComponent<InterestCardView>();
            Assign(view, "applicantNameLabel", applicantName);
            Assign(view, "messageLabel", message);
            Assign(view, "statusLabel", status);
            Assign(view, "acceptButton", acceptButton);
            Assign(view, "rejectButton", rejectButton);
            return view;
        }

        private static RectTransform CreateChatList(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            out ChatCardView template,
            out TMP_Text emptyLabel)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(CardColor.r, CardColor.g, CardColor.b, 0.35f);

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 45f;

            template = CreateChatCardTemplate(content);
            template.gameObject.SetActive(false);
            emptyLabel = CreateText(root, "EmptyState",
                "Aún no tienes conversaciones. Se crearán al aceptar una postulación.", 28f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                Vector2.zero, new Vector2(size.x - 100f, 180f));
            return content;
        }

        private static ChatCardView CreateChatCardTemplate(RectTransform parent)
        {
            RectTransform card = CreateRect("ChatCardTemplate", parent);
            card.sizeDelta = new Vector2(0f, 250f);
            Image image = card.gameObject.AddComponent<Image>();
            image.color = CardColor;
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 250f;
            layout.flexibleWidth = 1f;

            TMP_Text title = CreateText(card, "ProjectTitle", "Título del proyecto", 31f,
                FontStyles.Bold, TextAlignmentOptions.Left, TextColor,
                new Vector2(0f, 85f), new Vector2(790f, 50f));
            TMP_Text role = CreateText(card, "Role", "Participas como creador", 22f,
                FontStyles.Normal, TextAlignmentOptions.Left, PrimaryColor,
                new Vector2(0f, 42f), new Vector2(790f, 36f));
            TMP_Text lastMessage = CreateText(card, "LastMessage", "Último mensaje", 24f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor,
                new Vector2(-70f, -22f), new Vector2(650f, 75f));
            TMP_Text time = CreateText(card, "Time", "01/01 · 12:00", 20f,
                FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor,
                new Vector2(-270f, -92f), new Vector2(300f, 34f));
            Button openButton = CreateButton(card, "Button_OpenChat", "Abrir",
                new Vector2(285f, -88f), new Vector2(220f, 64f), SecondaryColor);

            ChatCardView view = card.gameObject.AddComponent<ChatCardView>();
            Assign(view, "projectTitleLabel", title);
            Assign(view, "roleLabel", role);
            Assign(view, "lastMessageLabel", lastMessage);
            Assign(view, "timeLabel", time);
            Assign(view, "openButton", openButton);
            return view;
        }

        private static RectTransform CreateChatMessageList(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            out ChatMessageView template,
            out TMP_Text emptyLabel,
            out ScrollRect scroll)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(CardColor.r, CardColor.g, CardColor.b, 0.22f);

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 16, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 48f;

            template = CreateChatMessageTemplate(content);
            template.gameObject.SetActive(false);
            emptyLabel = CreateText(root, "EmptyState",
                "No hay mensajes todavía. ¡Rompe el hielo!", 27f,
                FontStyles.Normal, TextAlignmentOptions.Center, MutedTextColor,
                Vector2.zero, new Vector2(size.x - 100f, 150f));
            return content;
        }

        private static ChatMessageView CreateChatMessageTemplate(RectTransform parent)
        {
            RectTransform root = CreateRect("ChatMessageTemplate", parent);
            root.sizeDelta = new Vector2(0f, 170f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 170f;
            layout.flexibleWidth = 1f;

            RectTransform bubble = CreateRect("Bubble", root);
            SetRect(bubble, Vector2.zero, new Vector2(700f, 150f));
            Image bubbleBackground = bubble.gameObject.AddComponent<Image>();
            bubbleBackground.color = SecondaryColor;
            TMP_Text sender = CreateText(bubble, "Sender", "Colaborador", 19f,
                FontStyles.Bold, TextAlignmentOptions.Left, TextColor,
                new Vector2(0f, 48f), new Vector2(620f, 28f));
            TMP_Text message = CreateText(bubble, "Message", "Mensaje", 25f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor,
                new Vector2(0f, 0f), new Vector2(620f, 75f));
            TMP_Text time = CreateText(bubble, "Time", "12:00", 18f,
                FontStyles.Normal, TextAlignmentOptions.Right, MutedTextColor,
                new Vector2(0f, -55f), new Vector2(620f, 25f));

            ChatMessageView view = root.gameObject.AddComponent<ChatMessageView>();
            Assign(view, "bubble", bubble);
            Assign(view, "bubbleBackground", bubbleBackground);
            Assign(view, "senderLabel", sender);
            Assign(view, "messageLabel", message);
            Assign(view, "timeLabel", time);
            return view;
        }

        private static RectTransform CreateScrollContent(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float contentHeight)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentHeight);

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 45f;
            return content;
        }

        private static TMP_InputField CreateInput(
            RectTransform parent,
            string name,
            string placeholderText,
            Vector2 position,
            TMP_InputField.ContentType contentType,
            int characterLimit)
        {
            return CreateInput(
                parent,
                name,
                placeholderText,
                position,
                new Vector2(760f, 106f),
                contentType,
                characterLimit);
        }

        private static TMP_InputField CreateInput(
            RectTransform parent,
            string name,
            string placeholderText,
            Vector2 position,
            Vector2 size,
            TMP_InputField.ContentType contentType,
            int characterLimit,
            bool multiline = false)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = FieldColor;

            TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.contentType = contentType;
            input.characterLimit = characterLimit;
            input.lineType = multiline
                ? TMP_InputField.LineType.MultiLineNewline
                : TMP_InputField.LineType.SingleLine;
            input.selectionColor = new Color(PrimaryColor.r, PrimaryColor.g, PrimaryColor.b, 0.45f);

            RectTransform viewport = CreateRect("Text Area", root);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(30f, 10f);
            viewport.offsetMax = new Vector2(-30f, -10f);
            viewport.gameObject.AddComponent<RectMask2D>();

            TextAlignmentOptions inputAlignment = multiline
                ? TextAlignmentOptions.TopLeft
                : TextAlignmentOptions.MidlineLeft;
            TMP_Text placeholder = CreateText(viewport, "Placeholder", placeholderText, 27f,
                FontStyles.Italic, inputAlignment, MutedTextColor,
                Vector2.zero, Vector2.zero);
            Stretch(placeholder.rectTransform);
            placeholder.color = new Color(MutedTextColor.r, MutedTextColor.g, MutedTextColor.b, 0.68f);

            TMP_Text text = CreateText(viewport, "Text", string.Empty, 28f,
                FontStyles.Normal, inputAlignment, TextColor,
                Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);

            input.textViewport = viewport;
            input.textComponent = (TextMeshProUGUI)text;
            input.placeholder = placeholder;
            return input;
        }

        private static Button CreateButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, position, size);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = PrimaryPressedColor;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.6f, 0.65f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text text = CreateText(root, "Label", label, 30f, FontStyles.Bold,
                TextAlignmentOptions.Center, TextColor, Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);
            return button;
        }

        private static TMP_Text CreateText(
            RectTransform parent,
            string name,
            string value,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            if (defaultFont != null)
            {
                text.font = defaultFont;
            }

            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"No existe la propiedad serializada '{propertyName}' en {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PutSceneFirstInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            scenes.AddRange(EditorBuildSettings.scenes.Where(scene => scene.path != ScenePath));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out Color color)
                ? color
                : Color.magenta;
        }
    }
}
