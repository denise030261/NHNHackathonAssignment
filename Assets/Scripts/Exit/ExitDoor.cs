using System.Collections;
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class ExitDoor : MonoBehaviour, IInteractable
    {
        [Header("Requirements")]
        [SerializeField, Min(1)] private int requiredKeys = 3;
        [SerializeField] private PlayerKeyInventory playerInventory;
        [SerializeField, Tooltip("Optional condition completed the first time this door is unlocked.")]
        private ProgressionCondition unlockedCondition;

        [Header("Door")]
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private float openAngle = 90f;
        [SerializeField, Min(0.01f)] private float openDuration = 1f;

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float lockedMessageDuration = 1.5f;

        private bool isAnimating;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool hasBeenUnlocked;

        public bool IsOpen { get; private set; }
        public string InteractionPrompt => IsOpen
            ? "\uBB38 \uB2EB\uAE30"
            : "\uBB38 \uC5F4\uAE30";

        private void Awake()
        {
            closedRotation = doorPanel.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return !isAnimating && interactor != null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (!IsOpen && !playerInventory.HasRequiredKeys(requiredKeys))
            {
                interactor.ShowTemporaryMessage(
                    $"\uC5F4\uC1E0\uAC00 \uBD80\uC871\uD569\uB2C8\uB2E4.  "
                    + $"{playerInventory.KeyCount} / {requiredKeys}",
                    lockedMessageDuration);
                return;
            }

            if (!IsOpen && !hasBeenUnlocked)
            {
                hasBeenUnlocked = true;
                GameProgressionController.Instance?.TryComplete(unlockedCondition);
            }

            StartCoroutine(AnimateDoor(!IsOpen));
        }

        private IEnumerator AnimateDoor(bool opening)
        {
            isAnimating = true;
            if (!opening)
            {
                IsOpen = false;
            }

            Quaternion startRotation = doorPanel.localRotation;
            Quaternion targetRotation = opening ? openRotation : closedRotation;
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
                doorPanel.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }

            doorPanel.localRotation = targetRotation;
            IsOpen = opening;
            isAnimating = false;
        }
    }
}
