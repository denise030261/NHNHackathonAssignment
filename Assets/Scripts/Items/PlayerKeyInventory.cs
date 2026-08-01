using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerKeyInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredKeyCount = 3;
        [SerializeField] private bool showKeyCounter = true;

        [Header("Key UI")]
        [SerializeField] private GameObject keyCounterRoot;
        [SerializeField] private Text keyCounterText;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private Text messageText;

        private readonly HashSet<string> collectedKeyIds = new HashSet<string>();
        private string temporaryMessage;
        private float messageExpiresAt;

        public event Action<int> KeyCountChanged;

        public int KeyCount => collectedKeyIds.Count;
        public int RequiredKeyCount => requiredKeyCount;

        public bool TryCollect(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId) || !collectedKeyIds.Add(keyId))
            {
                return false;
            }

            KeyCountChanged?.Invoke(KeyCount);
            RefreshUI();
            return true;
        }

        public bool HasRequiredKeys(int requiredCount)
        {
            return KeyCount >= requiredCount;
        }

        public void ShowDoorLockedMessage(int requiredCount, float duration)
        {
            temporaryMessage = $"NEED MORE KEYS  {KeyCount} / {requiredCount}";
            messageExpiresAt = Time.unscaledTime + duration;
            RefreshUI();
        }

        private void Awake()
        {
            RefreshUI();
        }

        private void Update()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (keyCounterRoot != null)
            {
                keyCounterRoot.SetActive(showKeyCounter);
            }
            if (keyCounterText != null)
            {
                keyCounterText.text = $"KEYS  {KeyCount} / {requiredKeyCount}";
            }

            bool showMessage = Time.unscaledTime < messageExpiresAt;
            if (messageRoot != null)
            {
                messageRoot.SetActive(showMessage);
            }
            if (messageText != null)
            {
                messageText.text = temporaryMessage;
            }
        }
    }
}
