using System.Collections.Generic;
using TecLink.Core;
using TecLink.FirebaseIntegration;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ChatListController : MonoBehaviour
    {
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private RectTransform cardsContent;
        [SerializeField] private ChatCardView cardTemplate;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;

        private readonly List<ChatCardView> cards = new List<ChatCardView>();
        private bool loading;

        private void Awake()
        {
            refreshButton.onClick.AddListener(Refresh);
            backButton.onClick.AddListener(OnBackPressed);
        }

        private void OnEnable()
        {
            Refresh();
        }

        public async void Refresh()
        {
            if (loading)
            {
                return;
            }

            loading = true;
            SetInteractable(false);
            AppManager.Instance.SetBusy(true, "Cargando conversaciones...");
            ChatQueryResult result = await FirestoreService.Instance.LoadCurrentUserChatsAsync();
            AppManager.Instance.SetBusy(false);
            SetInteractable(true);
            loading = false;

            if (!result.Success)
            {
                AppManager.Instance.ShowError(result.ErrorMessage);
                return;
            }

            ClearCards();
            string currentUserId = AuthService.Instance?.CurrentUser?.UserId ?? string.Empty;
            foreach (ChatRecord chat in result.Chats)
            {
                ChatCardView card = Instantiate(cardTemplate, cardsContent);
                card.gameObject.SetActive(true);
                card.Bind(chat, currentUserId, OnChatSelected);
                cards.Add(card);
            }

            emptyLabel.gameObject.SetActive(result.Chats.Count == 0);
        }

        private void OnChatSelected(ChatRecord chat)
        {
            if (loading)
            {
                return;
            }

            SessionManager.Instance.SelectChat(chat);
            NavigationManager.Instance.ShowChat();
        }

        private void OnBackPressed()
        {
            if (!loading)
            {
                NavigationManager.Instance.ShowFeed();
            }
        }

        private void SetInteractable(bool interactable)
        {
            refreshButton.interactable = interactable;
            backButton.interactable = interactable;
            foreach (ChatCardView card in cards)
            {
                card?.SetInteractable(interactable);
            }
        }

        private void ClearCards()
        {
            foreach (ChatCardView card in cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            cards.Clear();
        }

        private void OnDestroy()
        {
            refreshButton.onClick.RemoveListener(Refresh);
            backButton.onClick.RemoveListener(OnBackPressed);
        }
    }
}
