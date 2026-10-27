using System;
using TecLink.Core;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class InterestCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text applicantNameLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        private InterestRecord record;
        private Action<InterestRecord> acceptAction;
        private Action<InterestRecord> rejectAction;

        private void Awake()
        {
            acceptButton.onClick.AddListener(OnAcceptPressed);
            rejectButton.onClick.AddListener(OnRejectPressed);
        }

        public void Bind(
            InterestRecord value,
            Action<InterestRecord> onAccept,
            Action<InterestRecord> onReject)
        {
            record = value;
            acceptAction = onAccept;
            rejectAction = onReject;
            ProjectInterest data = record.Data;
            applicantNameLabel.text = data.ApplicantName;
            messageLabel.text = data.Message;
            statusLabel.text = DisplayStatus(data.Status);
            bool pending = data.Status == AppConstants.InterestStatusPending;
            acceptButton.gameObject.SetActive(pending);
            rejectButton.gameObject.SetActive(pending);
        }

        public void SetInteractable(bool interactable)
        {
            acceptButton.interactable = interactable;
            rejectButton.interactable = interactable;
        }

        public static string DisplayStatus(string status)
        {
            switch (status)
            {
                case AppConstants.InterestStatusAccepted:
                    return "Aceptada · chat creado";
                case AppConstants.InterestStatusRejected:
                    return "Rechazada";
                default:
                    return "Pendiente";
            }
        }

        private void OnAcceptPressed()
        {
            if (record != null)
            {
                acceptAction?.Invoke(record);
            }
        }

        private void OnRejectPressed()
        {
            if (record != null)
            {
                rejectAction?.Invoke(record);
            }
        }

        private void OnDestroy()
        {
            acceptButton.onClick.RemoveListener(OnAcceptPressed);
            rejectButton.onClick.RemoveListener(OnRejectPressed);
        }
    }
}
