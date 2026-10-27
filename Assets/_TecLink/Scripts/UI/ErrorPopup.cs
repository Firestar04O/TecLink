using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ErrorPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(string message)
        {
            messageLabel.text = string.IsNullOrWhiteSpace(message)
                ? "Ocurrió un error inesperado."
                : message;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            closeButton.Select();
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }
}
