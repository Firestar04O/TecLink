using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ChatMessageView : MonoBehaviour
    {
        [SerializeField] private RectTransform bubble;
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private TMP_Text senderLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text timeLabel;

        private static readonly Color OwnColor = new Color(0.086f, 0.722f, 0.651f, 1f);
        private static readonly Color OtherColor = new Color(0.129f, 0.227f, 0.329f, 1f);

        public void Bind(ChatMessageRecord record, string currentUserId)
        {
            ChatMessage data = record.Data;
            bool own = data.SenderId == currentUserId;
            senderLabel.text = own ? "Tú" : "Colaborador";
            messageLabel.text = data.Text;
            timeLabel.text = data.CreatedAt.ToDateTime().ToLocalTime().ToString("HH:mm");
            bubbleBackground.color = own ? OwnColor : OtherColor;

            bubble.anchorMin = new Vector2(own ? 1f : 0f, 0.5f);
            bubble.anchorMax = new Vector2(own ? 1f : 0f, 0.5f);
            bubble.pivot = new Vector2(own ? 1f : 0f, 0.5f);
            bubble.anchoredPosition = new Vector2(own ? -16f : 16f, 0f);

            TextAlignmentOptions alignment = own
                ? TextAlignmentOptions.Right
                : TextAlignmentOptions.Left;
            senderLabel.alignment = alignment;
            messageLabel.alignment = alignment;
            timeLabel.alignment = alignment;

            float textHeight = messageLabel.GetPreferredValues(data.Text, 620f, 0f).y;
            float bubbleHeight = Mathf.Clamp(textHeight + 92f, 135f, 430f);
            bubble.sizeDelta = new Vector2(700f, bubbleHeight);
            senderLabel.rectTransform.anchoredPosition =
                new Vector2(0f, bubbleHeight * 0.5f - 25f);
            messageLabel.rectTransform.anchoredPosition = Vector2.zero;
            messageLabel.rectTransform.sizeDelta =
                new Vector2(620f, Mathf.Min(textHeight + 8f, bubbleHeight - 82f));
            timeLabel.rectTransform.anchoredPosition =
                new Vector2(0f, -bubbleHeight * 0.5f + 20f);
            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = bubbleHeight + 18f;
            }
        }
    }
}
