using System;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ChatCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text projectTitleLabel;
        [SerializeField] private TMP_Text roleLabel;
        [SerializeField] private TMP_Text lastMessageLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Button openButton;

        private ChatRecord record;
        private Action<ChatRecord> openAction;

        private void Awake()
        {
            openButton.onClick.AddListener(OnOpenPressed);
        }

        public void Bind(ChatRecord value, string currentUserId, Action<ChatRecord> onOpen)
        {
            record = value;
            openAction = onOpen;
            ChatData data = record.Data;
            projectTitleLabel.text = data.ProjectTitle;
            roleLabel.text = data.OwnerId == currentUserId
                ? "Participas como creador"
                : "Participas como postulante";
            lastMessageLabel.text = string.IsNullOrWhiteSpace(data.LastMessage)
                ? "Aún no hay mensajes. Inicia la conversación."
                : data.LastMessage;
            timeLabel.text = data.LastMessageAt.ToDateTime().ToLocalTime().ToString("dd/MM · HH:mm");
        }

        public void SetInteractable(bool interactable)
        {
            openButton.interactable = interactable;
        }

        private void OnOpenPressed()
        {
            if (record != null)
            {
                openAction?.Invoke(record);
            }
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(OnOpenPressed);
        }
    }
}
