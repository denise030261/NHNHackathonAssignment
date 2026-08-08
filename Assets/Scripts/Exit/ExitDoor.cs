using System.Collections;
using System.Collections.Generic;
using NHNHackathon.AudioSystem;
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
        [SerializeField, Tooltip("Exact key items required to unlock this door.")]
        private List<ItemDefinition> requiredKeys = new List<ItemDefinition>();
        [SerializeField, Tooltip("Remove the required keys from inventory on the first unlock.")]
        private bool consumeKeysOnUnlock;
        [SerializeField] private PlayerItemInventory playerInventory;
        [SerializeField, Tooltip("Optional condition completed the first time this door is unlocked.")]
        private ProgressionCondition unlockedCondition;

        [Header("Door")]
        [SerializeField, Tooltip("Disable direct E interaction when another controller manages this door.")]
        private bool directInteractionEnabled = true;
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
        private bool isPermanentlySealed;

        public bool IsOpen { get; private set; }
        public bool IsAnimating => isAnimating;
        public bool IsPermanentlySealed => isPermanentlySealed;
        public bool IsUnlocked => hasBeenUnlocked || requiredKeys.Count == 0;
        public string InteractionPrompt => IsOpen
            ? "\uBB38 \uB2EB\uAE30"
            : "\uBB38 \uC5F4\uAE30";

        private void Awake()
        {
            if (doorPanel == null)
            {
                doorPanel = transform;
            }
            closedRotation = doorPanel.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return directInteractionEnabled && !isPermanentlySealed
                && !isAnimating && interactor != null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (!IsOpen && !hasBeenUnlocked)
            {
                playerInventory ??= interactor.GetComponent<PlayerItemInventory>();
                if (!HasRequiredKeys())
                {
                    interactor.ShowTemporaryMessage(
                        BuildLockedMessage(), lockedMessageDuration);
                    return;
                }

                if (consumeKeysOnUnlock && requiredKeys.Count > 0)
                {
                    if (!playerInventory.TryConsume(requiredKeys))
                    {
                        interactor.ShowTemporaryMessage(
                            BuildLockedMessage(), lockedMessageDuration);
                        return;
                    }
                }
                hasBeenUnlocked = true;
                GameProgressionController.Instance?.TryComplete(unlockedCondition);
            }

            GameSfxPlayer.PlayRegularDoor(doorPanel.position);
            StartCoroutine(AnimateDoor(!IsOpen, openDuration));
        }

        public bool TryClose(float duration)
        {
            if (!IsOpen || isAnimating)
            {
                return false;
            }

            StartCoroutine(AnimateDoor(false, Mathf.Max(0.01f, duration)));
            return true;
        }

        public bool TryOpen(float duration)
        {
            if (IsOpen || isAnimating || isPermanentlySealed)
            {
                return false;
            }

            hasBeenUnlocked = true;
            GameProgressionController.Instance?.TryComplete(unlockedCondition);
            StartCoroutine(AnimateDoor(true, Mathf.Max(0.01f, duration)));
            return true;
        }

        public bool TrySlamAndSeal(float duration)
        {
            if (!TryClose(duration))
            {
                return false;
            }

            isPermanentlySealed = true;
            return true;
        }

        private bool HasRequiredKeys()
        {
            if (requiredKeys.Count == 0)
            {
                return true;
            }
            if (playerInventory == null)
            {
                return false;
            }

            foreach (ItemDefinition key in requiredKeys)
            {
                if (key == null || !playerInventory.Contains(key))
                {
                    return false;
                }
            }
            return true;
        }

        private string BuildLockedMessage()
        {
            if (requiredKeys.Count == 1 && requiredKeys[0] != null)
            {
                return $"{requiredKeys[0].DisplayName}\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4.";
            }

            int ownedCount = 0;
            foreach (ItemDefinition key in requiredKeys)
            {
                if (key != null && playerInventory != null && playerInventory.Contains(key))
                {
                    ownedCount++;
                }
            }
            return $"\uD544\uC694\uD55C \uC5F4\uC1E0\uAC00 \uBD80\uC871\uD569\uB2C8\uB2E4.  {ownedCount} / {requiredKeys.Count}";
        }

        private void OnValidate()
        {
            if (doorPanel == null)
            {
                Debug.LogWarning($"{name}: Door Panel is not assigned.", this);
            }

            foreach (ItemDefinition key in requiredKeys)
            {
                if (key != null && key.Type != ItemType.Key)
                {
                    Debug.LogWarning(
                        $"{name}: Required Keys accepts only Key ItemDefinitions.", this);
                }
            }
        }

        private IEnumerator AnimateDoor(bool opening, float duration)
        {
            isAnimating = true;
            if (!opening)
            {
                IsOpen = false;
            }

            Quaternion startRotation = doorPanel.localRotation;
            Quaternion targetRotation = opening ? openRotation : closedRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                doorPanel.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }

            doorPanel.localRotation = targetRotation;
            IsOpen = opening;
            isAnimating = false;

            if (!opening)
            {
                // TODO(Audio): 문 닫힘 SFX 소스가 준비되면 여기서 쾅 닫히는 효과음을 재생한다.
                // slamAudioSource.PlayOneShot(slamAudioClip);
            }
        }
    }
}
