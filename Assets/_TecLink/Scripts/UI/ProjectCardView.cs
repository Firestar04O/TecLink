using System;
using System.Collections.Generic;
using TecLink.Core;
using TecLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecLink.UI
{
    [DisallowMultipleComponent]
    public sealed class ProjectCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text ownerLabel;
        [SerializeField] private TMP_Text metadataLabel;
        [SerializeField] private TMP_Text skillsLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button openButton;

        private ProjectRecord record;
        private Action<ProjectRecord> openAction;

        private void Awake()
        {
            openButton.onClick.AddListener(OnOpenPressed);
        }

        public void Bind(ProjectRecord value, Action<ProjectRecord> onOpen)
        {
            record = value;
            openAction = onOpen;
            ProjectData data = record.Data;
            titleLabel.text = data.Title;
            ownerLabel.text = string.IsNullOrWhiteSpace(data.OwnerCareer)
                ? $"Por {data.OwnerName}"
                : $"Por {data.OwnerName} · {data.OwnerCareer}";
            metadataLabel.text = $"{DisplayModality(data.Modality)} · {data.Campus} · " +
                                 $"{data.HoursPerWeek} h/sem";
            skillsLabel.text = $"Busca: {string.Join(", ", data.RequiredSkills ?? new List<string>())}";
            statusLabel.text = DisplayStatus(data.Status);
        }

        private void OnOpenPressed()
        {
            if (record != null)
            {
                openAction?.Invoke(record);
            }
        }

        public static string DisplayStatus(string status)
        {
            switch (status)
            {
                case AppConstants.ProjectStatusSelecting:
                    return "Seleccionando";
                case AppConstants.ProjectStatusFilled:
                    return "Equipo completo";
                case AppConstants.ProjectStatusClosed:
                    return "Cerrado";
                default:
                    return "Abierto";
            }
        }

        public static string DisplayModality(string modality)
        {
            switch (modality)
            {
                case AppConstants.ProjectModalityRemote:
                    return "Remoto";
                case AppConstants.ProjectModalityHybrid:
                    return "Híbrido";
                default:
                    return "Presencial";
            }
        }

        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(OnOpenPressed);
        }
    }
}
