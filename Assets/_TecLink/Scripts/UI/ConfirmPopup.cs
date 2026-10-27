using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ConfirmPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action confirmAction;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmPressed);
            cancelButton.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(string message, Action onConfirm)
        {
            messageLabel.text = string.IsNullOrWhiteSpace(message)
                ? "¿Deseas continuar?"
                : message;
            confirmAction = onConfirm;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            cancelButton.Select();
        }

        public void Hide()
        {
            confirmAction = null;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnConfirmPressed()
        {
            Action action = confirmAction;
            Hide();
            action?.Invoke();
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(OnConfirmPressed);
            cancelButton.onClick.RemoveListener(Hide);
        }
    }
}
