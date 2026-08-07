using System;
using System.Collections.Generic;
using NHNHackathon.Inspection;
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [Serializable]
    public sealed class PaperDanceUnlockRule
    {
        [SerializeField, Tooltip("The paper whose second page unlocks this dance.")]
        private ItemDefinition paper;

        [SerializeField, Min(1)] private int danceId = 1;

        public ItemDefinition Paper => paper;
        public int DanceId => danceId;
    }

    [DisallowMultipleComponent]
    public sealed class PlayerDanceUnlockController : MonoBehaviour
    {
        private const int SecondPageIndex = 1;

        [Header("Paper Reader")]
        [SerializeField, Tooltip("Optional. If empty, the scene's ItemInspectionController is found automatically.")]
        private ItemInspectionController inspectionController;

        [Header("Unlock Rules")]
        [SerializeField, Tooltip("Each rule maps a paper to the dance unlocked when its second page is opened.")]
        private List<PaperDanceUnlockRule> unlockRules = new List<PaperDanceUnlockRule>();

        [SerializeField, Tooltip("Dances available before any paper is read.")]
        private List<int> initiallyUnlockedDanceIds = new List<int>();

        [Header("Messages")]
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private bool showUnlockMessage = true;
        [SerializeField] private string unlockMessageFormat = "춤 {0}을(를) 해금했습니다.";
        [SerializeField] private string lockedMessageFormat = "춤 {0}은(는) 아직 해금되지 않았습니다.";
        [SerializeField, Min(0.1f)] private float messageDuration = 1.5f;

        private readonly HashSet<int> unlockedDanceIds = new HashSet<int>();

        public event Action<int> DanceUnlocked;

        private void Awake()
        {
            foreach (int danceId in initiallyUnlockedDanceIds)
            {
                if (danceId > 0)
                {
                    unlockedDanceIds.Add(danceId);
                }
            }

            if (playerInteractor == null)
            {
                playerInteractor = GetComponent<PlayerInteractor>();
            }
        }

        private void Start()
        {
            if (inspectionController == null)
            {
                inspectionController = FindObjectOfType<ItemInspectionController>(true);
            }

            if (inspectionController != null)
            {
                inspectionController.PaperPageOpened += HandlePaperPageOpened;
            }
        }

        private void OnDestroy()
        {
            if (inspectionController != null)
            {
                inspectionController.PaperPageOpened -= HandlePaperPageOpened;
            }
        }

        public bool IsUnlocked(int danceId)
        {
            return unlockedDanceIds.Contains(danceId);
        }

        public void NotifyLockedDance(int danceId)
        {
            if (playerInteractor != null && !string.IsNullOrWhiteSpace(lockedMessageFormat))
            {
                playerInteractor.ShowTemporaryMessage(
                    string.Format(lockedMessageFormat, danceId), messageDuration);
            }
        }

        private void HandlePaperPageOpened(ItemDefinition paper, int pageIndex)
        {
            if (paper == null || pageIndex != SecondPageIndex)
            {
                return;
            }

            foreach (PaperDanceUnlockRule rule in unlockRules)
            {
                if (rule == null || rule.Paper != paper || rule.DanceId < 1)
                {
                    continue;
                }

                if (unlockedDanceIds.Add(rule.DanceId))
                {
                    DanceUnlocked?.Invoke(rule.DanceId);
                    ShowUnlockMessage(rule.DanceId);
                }
            }
        }

        private void ShowUnlockMessage(int danceId)
        {
            if (!showUnlockMessage
                || playerInteractor == null
                || string.IsNullOrWhiteSpace(unlockMessageFormat))
            {
                return;
            }

            playerInteractor.ShowTemporaryMessage(
                string.Format(unlockMessageFormat, danceId), messageDuration);
        }

        private void OnValidate()
        {
            messageDuration = Mathf.Max(0.1f, messageDuration);
        }
    }
}
