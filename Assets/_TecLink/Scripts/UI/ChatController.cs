using System.Collections.Generic;
using Firebase.Firestore;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ChatController : MonoBehaviour
    {
        [SerializeField] private TMP_Text projectTitleLabel;
        [SerializeField] private TMP_Text roleLabel;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private RectTransform messagesContent;
        [SerializeField] private ChatMessageView messageTemplate;
        [SerializeField] private ScrollRect messagesScroll;
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private TMP_Text characterCountLabel;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button backButton;

        private readonly List<ChatMessageView> messageViews = new List<ChatMessageView>();
        private ListenerRegistration listener;
        private bool sending;
        private bool listenerErrorShown;

        private void Awake()
        {
            sendButton.onClick.AddListener(OnSendPressed);
            backButton.onClick.AddListener(OnBackPressed);
            messageInput.onValueChanged.AddListener(OnMessageChanged);
        }

        private void OnEnable()
        {
            ChatRecord chat = SessionManager.Instance?.SelectedChat;
            string currentUserId = AuthService.Instance?.CurrentUser?.UserId;
            if (chat?.Data == null || string.IsNullOrWhiteSpace(currentUserId) ||
                !chat.Data.MemberIds.Contains(currentUserId))
            {
                AppManager.Instance.ShowError("No se pudo abrir esta conversación.");
                NavigationManager.Instance.ShowChats();
                return;
            }

            projectTitleLabel.text = chat.Data.ProjectTitle;
            roleLabel.text = chat.Data.OwnerId == currentUserId
                ? "Conversación con el postulante"
                : "Conversación con el creador";
            messageInput.text = string.Empty;
            listenerErrorShown = false;
            UpdateCharacterCount();
            StartListener(chat.Id);
        }

        private void OnDisable()
        {
            StopListener();
            ClearMessages();
        }

        private void StartListener(string chatId)
        {
            StopListener();
            AppManager.Instance.SetBusy(true, "Abriendo conversación...");
            listener = FirestoreService.Instance.ListenToChatMessages(chatId, OnMessagesChanged);
            if (listener == null)
            {
                AppManager.Instance.SetBusy(false);
            }
        }

        private void OnMessagesChanged(MessageQueryResult result)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            AppManager.Instance.SetBusy(false);
            if (!result.Success)
            {
                if (!listenerErrorShown)
                {
                    listenerErrorShown = true;
                    AppManager.Instance.ShowError(result.ErrorMessage);
                }
                return;
            }

            RenderMessages(result.Messages);
        }

        private void RenderMessages(List<ChatMessageRecord> messages)
        {
            ClearMessages();
            string currentUserId = AuthService.Instance?.CurrentUser?.UserId ?? string.Empty;
            foreach (ChatMessageRecord message in messages)
            {
                ChatMessageView view = Instantiate(messageTemplate, messagesContent);
                view.gameObject.SetActive(true);
                view.Bind(message, currentUserId);
                messageViews.Add(view);
            }

            emptyLabel.gameObject.SetActive(messages.Count == 0);
            Canvas.ForceUpdateCanvases();
            messagesScroll.verticalNormalizedPosition = 0f;
        }

        public async void OnSendPressed()
        {
            if (sending)
            {
                return;
            }

            ChatRecord chat = SessionManager.Instance?.SelectedChat;
            if (chat == null)
            {
                AppManager.Instance.ShowError("No hay una conversación seleccionada.");
                return;
            }

            string text = (messageInput.text ?? string.Empty).Trim();
            if (text.Length < 1 || text.Length > AppConstants.MaximumChatMessageLength)
            {
                AppManager.Instance.ShowError(
                    $"El mensaje debe tener entre 1 y {AppConstants.MaximumChatMessageLength} caracteres.");
                return;
            }

            sending = true;
            SetInteractable(false);
            ChatOperationResult result =
                await FirestoreService.Instance.SendChatMessageAsync(chat.Id, text);
            SetInteractable(true);
            sending = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            messageInput.text = string.Empty;
            messageInput.ActivateInputField();
        }

        private void OnBackPressed()
        {
            if (!sending)
            {
                NavigationManager.Instance.ShowChats();
            }
        }

        private void OnMessageChanged(string value)
        {
            UpdateCharacterCount();
        }

        private void UpdateCharacterCount()
        {
            int count = (messageInput.text ?? string.Empty).Length;
            characterCountLabel.text = $"{count}/{AppConstants.MaximumChatMessageLength}";
        }

        private void SetInteractable(bool interactable)
        {
            sendButton.interactable = interactable;
            backButton.interactable = interactable;
            messageInput.interactable = interactable;
        }

        private void StopListener()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
        }

        private void ClearMessages()
        {
            foreach (ChatMessageView view in messageViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            messageViews.Clear();
        }

        private void OnDestroy()
        {
            StopListener();
            sendButton.onClick.RemoveListener(OnSendPressed);
            backButton.onClick.RemoveListener(OnBackPressed);
            messageInput.onValueChanged.RemoveListener(OnMessageChanged);
        }
    }
}
