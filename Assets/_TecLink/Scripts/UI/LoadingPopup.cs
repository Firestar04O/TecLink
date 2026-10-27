using TMPro;
using UnityEngine;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class LoadingPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageLabel;

        private void Awake()
        {
            Hide();
        }

        public void Show(string message)
        {
            messageLabel.text = string.IsNullOrWhiteSpace(message) ? "Procesando..." : message;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
