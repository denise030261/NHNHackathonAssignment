using System;
using System.Collections.Generic;
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [Serializable]
    public sealed class PaperDanceUnlockRule
    {
        [SerializeField, Tooltip("The paper item that unlocks this dance when collected.")]
        private ItemDefinition paper;

        [SerializeField, Min(1)] private int danceId = 1;

        public ItemDefinition Paper => paper;
        public int DanceId => danceId;
    }

    [DisallowMultipleComponent]
    public sealed class PlayerDanceUnlockController : MonoBehaviour
    {
        [Header("Unlock Rules")]
        [SerializeField, Tooltip("Each rule maps a collected paper item to the dance it unlocks.")]
        private List<PaperDanceUnlockRule> unlockRules = new List<PaperDanceUnlockRule>();

        [SerializeField, Tooltip("Dances available before any paper is collected.")]
        private List<int> initiallyUnlockedDanceIds = new List<int>();

        [Header("Inventory")]
        [SerializeField] private PlayerItemInventory playerInventory;

        [Header("Messages")]
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private bool showUnlockMessage = true;
        [SerializeField] private string unlockMessageFormat = "춤 {0}을(를) 해금했습니다.";
        [SerializeField] private string lockedMessageFormat = "춤 {0}은(는) 아직 해금되지 않았습니다.";
        [SerializeField, Min(0.1f)] private float messageDuration = 1.5f;

        private readonly HashSet<int> unlockedDanceIds = new HashSet<int>();

        public event Action<int> DanceUnlocked;
        public IReadOnlyCollection<int> UnlockedDanceIds => unlockedDanceIds;

        private void Awake()
        {
            playerInventory ??= GetComponent<PlayerItemInventory>();
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

        private void OnEnable()
        {
            playerInventory ??= GetComponent<PlayerItemInventory>();
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void Start()
        {
            UnlockCollectedPapers();
        }

        private void OnDisable()
        {
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }
        }

        public bool IsUnlocked(int danceId)
        {
            return unlockedDanceIds.Contains(danceId);
        }

        public void UnlockAll()
        {
            foreach (PaperDanceUnlockRule rule in unlockRules)
            {
                if (rule != null && rule.DanceId > 0 && unlockedDanceIds.Add(rule.DanceId))
                {
                    DanceUnlocked?.Invoke(rule.DanceId);
                }
            }
        }

        public void NotifyLockedDance(int danceId)
        {
            if (playerInteractor != null && !string.IsNullOrWhiteSpace(lockedMessageFormat))
            {
                playerInteractor.ShowTemporaryMessage(
                    string.Format(lockedMessageFormat, danceId), messageDuration);
            }
        }

        public void Restore(IEnumerable<int> danceIds)
        {
            unlockedDanceIds.Clear();
            foreach (int danceId in initiallyUnlockedDanceIds)
            {
                if (danceId > 0)
                {
                    unlockedDanceIds.Add(danceId);
                }
            }
            if (danceIds == null)
            {
                return;
            }
            foreach (int danceId in danceIds)
            {
                if (danceId > 0)
                {
                    unlockedDanceIds.Add(danceId);
                }
            }
        }

        private void HandleInventoryChanged()
        {
            UnlockCollectedPapers();
        }

        private void UnlockCollectedPapers()
        {
            if (playerInventory == null)
            {
                return;
            }

            foreach (PaperDanceUnlockRule rule in unlockRules)
            {
                if (rule == null || rule.Paper == null || rule.DanceId < 1
                    || !playerInventory.Contains(rule.Paper))
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
