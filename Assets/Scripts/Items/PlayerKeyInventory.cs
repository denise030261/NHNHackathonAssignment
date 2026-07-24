using System;
using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerKeyInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredKeyCount = 3;
        [SerializeField] private bool showKeyCounter = true;

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
            return true;
        }

        public bool HasRequiredKeys(int requiredCount)
        {
            return KeyCount >= requiredCount;
        }

        public void ShowDoorLockedMessage(int requiredCount, float duration)
        {
            temporaryMessage = $"NEED MORE KEYS  {KeyCount} / {requiredCount}";
            messageExpiresAt = Time.time + duration;
        }

        private void OnGUI()
        {
            GUIStyle counterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            counterStyle.normal.textColor = Color.white;

            if (showKeyCounter)
            {
                GUI.Label(
                    new Rect(30f, 25f, 320f, 50f),
                    $"KEYS  {KeyCount} / {requiredKeyCount}",
                    counterStyle);
            }

            if (Time.time >= messageExpiresAt)
            {
                return;
            }

            GUIStyle messageStyle = new GUIStyle(counterStyle)
            {
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter
            };
            messageStyle.normal.textColor = new Color(1f, 0.75f, 0.2f);
            GUI.Label(
                new Rect(0f, Screen.height * 0.68f, Screen.width, 60f),
                temporaryMessage,
                messageStyle);
        }
    }
}
