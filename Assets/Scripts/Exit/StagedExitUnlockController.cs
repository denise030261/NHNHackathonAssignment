using System;
using System.Collections.Generic;
using NHNHackathon.AudioSystem;
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using NHNHackathon.MainMenu;
using UnityEngine;
using UnityEngine.Events;

namespace NHNHackathon.ExitSystem
{
    [Serializable]
    public sealed class ExitUnlockStage
    {
        [SerializeField] private string displayName = "잠금장치 해제 중";
        [SerializeField] private ItemDefinition requiredKey;
        [SerializeField, Min(0.1f)] private float unlockDuration = 2f;
        [SerializeField] private bool consumeKeyOnComplete;
        [SerializeField] private UnityEvent onCompleted = new();

        public string DisplayName => displayName;
        public ItemDefinition RequiredKey => requiredKey;
        public float UnlockDuration => Mathf.Max(0.1f, unlockDuration);
        public bool ConsumeKeyOnComplete => consumeKeyOnComplete;
        public UnityEvent OnCompleted => onCompleted;
    }

    [DisallowMultipleComponent]
    public sealed class StagedExitUnlockController : MonoBehaviour, IInteractable
    {
        [Header("Input")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField, Min(0f)] private float movementTolerance = 0.03f;

        [Header("Stages")]
        [SerializeField] private List<ExitUnlockStage> stages = new();

        [Header("References")]
        [SerializeField] private ExitUnlockProgressUI progressUI;

        [Header("Unlock Loop SFX")]
        [SerializeField] private AudioClip unlockLoopClip;
        [SerializeField] private AudioSource unlockLoopAudioSource;
        [SerializeField, Range(0f, 1f)] private float unlockLoopVolumeScale = 1f;
        [SerializeField, Range(0f, 1f)] private float unlockLoopSpatialBlend;
        [SerializeField, Min(0f)] private float unlockLoopMinDistance = 1f;
        [SerializeField, Min(0.1f)] private float unlockLoopMaxDistance = 18f;

        private PlayerInteractor activeInteractor;
        private PlayerItemInventory inventory;
        private Vector3 startPosition;
        private float elapsed;
        private int currentStageIndex;
        private bool isUnlocking;

        public IReadOnlyList<ExitUnlockStage> Stages => stages;

        public string InteractionPrompt => "잠금장치 해제 (E 길게 누르기)";

        private void Update()
        {
            if (!isUnlocking)
            {
                return;
            }

            bool hasMovementInput = Mathf.Abs(UnityEngine.Input.GetAxisRaw("Horizontal")) > 0.01f
                || Mathf.Abs(UnityEngine.Input.GetAxisRaw("Vertical")) > 0.01f;
            bool moved = activeInteractor == null
                || Vector3.Distance(startPosition, activeInteractor.transform.position) > movementTolerance;
            if (!UnityEngine.Input.GetKey(interactionKey) || hasMovementInput || moved)
            {
                CancelUnlock();
                return;
            }

            ExitUnlockStage stage = stages[currentStageIndex];
            elapsed = Mathf.Min(elapsed + Time.deltaTime, stage.UnlockDuration);
            progressUI?.Show(stage.DisplayName, elapsed, stage.UnlockDuration);
            if (elapsed >= stage.UnlockDuration)
            {
                CompleteCurrentStage(stage);
            }
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return !isUnlocking && interactor != null
                && currentStageIndex >= 0 && currentStageIndex < stages.Count;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            ExitUnlockStage stage = stages[currentStageIndex];
            inventory = interactor.GetComponent<PlayerItemInventory>();
            if (inventory == null || stage.RequiredKey == null || !inventory.Contains(stage.RequiredKey))
            {
                string keyName = stage.RequiredKey != null ? stage.RequiredKey.DisplayName : "필요한 열쇠";
                interactor.ShowTemporaryMessage($"{keyName}가 필요합니다.", 1.5f);
                return;
            }

            activeInteractor = interactor;
            startPosition = interactor.transform.position;
            isUnlocking = true;
            progressUI?.Show(stage.DisplayName, elapsed, stage.UnlockDuration);
            PlayUnlockLoopSfx();
        }

        private void CompleteCurrentStage(ExitUnlockStage stage)
        {
            if (stage.ConsumeKeyOnComplete)
            {
                inventory.TryConsume(new[] { stage.RequiredKey });
            }
            stage.OnCompleted?.Invoke();
            currentStageIndex++;
            elapsed = 0f;
            FinishAttempt();
        }

        private void CancelUnlock()
        {
            FinishAttempt();
        }

        private void FinishAttempt()
        {
            StopUnlockLoopSfx();
            isUnlocking = false;
            activeInteractor = null;
            inventory = null;
            progressUI?.Hide();
        }

        private void PlayUnlockLoopSfx()
        {
            AudioClip clip = unlockLoopClip != null
                ? unlockLoopClip
                : GameSfxPlayer.Library != null
                    ? GameSfxPlayer.Library.ExitUnlockLoop
                    : null;
            if (clip == null)
            {
                Debug.LogWarning(
                    "Unlock loop SFX is not assigned.", this);
                return;
            }

            EnsureUnlockLoopAudioSource();
            unlockLoopAudioSource.Stop();
            unlockLoopAudioSource.clip = clip;
            unlockLoopAudioSource.loop = true;
            unlockLoopAudioSource.playOnAwake = false;
            unlockLoopAudioSource.spatialBlend = unlockLoopSpatialBlend;
            unlockLoopAudioSource.minDistance = unlockLoopMinDistance;
            unlockLoopAudioSource.maxDistance = Mathf.Max(
                unlockLoopMinDistance + 0.1f, unlockLoopMaxDistance);
            unlockLoopAudioSource.volume = Mathf.Clamp01(
                AudioSettingsController.SavedSfxVolume * unlockLoopVolumeScale);
            unlockLoopAudioSource.Play();
        }

        private void EnsureUnlockLoopAudioSource()
        {
            if (unlockLoopAudioSource != null)
            {
                return;
            }

            unlockLoopAudioSource = gameObject.AddComponent<AudioSource>();
            unlockLoopAudioSource.playOnAwake = false;
            unlockLoopAudioSource.loop = true;
            unlockLoopAudioSource.dopplerLevel = 0f;
        }

        private void StopUnlockLoopSfx()
        {
            if (unlockLoopAudioSource != null && unlockLoopAudioSource.isPlaying)
            {
                unlockLoopAudioSource.Stop();
            }
        }

        private void OnDisable()
        {
            CancelUnlock();
        }
    }
}
