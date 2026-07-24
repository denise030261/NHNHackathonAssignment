using System.Collections;
using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class ExitDoor : MonoBehaviour
    {
        [Header("Requirements")]
        [SerializeField, Min(1)] private int requiredKeys = 3;
        [SerializeField] private PlayerKeyInventory playerInventory;

        [Header("Door")]
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private float openAngle = 90f;
        [SerializeField, Min(0.01f)] private float openDuration = 1f;

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float lockedMessageDuration = 1.5f;

        private bool isOpening;

        public bool IsOpen { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            if (IsOpen || isOpening)
            {
                return;
            }

            PlayerKeyInventory inventory = other.GetComponentInParent<PlayerKeyInventory>();
            if (inventory == null || inventory != playerInventory)
            {
                return;
            }

            if (!inventory.HasRequiredKeys(requiredKeys))
            {
                inventory.ShowDoorLockedMessage(requiredKeys, lockedMessageDuration);
                return;
            }

            StartCoroutine(OpenDoor());
        }

        private IEnumerator OpenDoor()
        {
            isOpening = true;
            Quaternion startRotation = doorPanel.localRotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(0f, openAngle, 0f);
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
                doorPanel.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }

            doorPanel.localRotation = targetRotation;
            if (blockingCollider != null)
            {
                blockingCollider.enabled = false;
            }

            IsOpen = true;
            isOpening = false;
        }
    }
}
