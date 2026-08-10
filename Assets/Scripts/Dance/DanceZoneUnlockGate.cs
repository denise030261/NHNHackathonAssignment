using System.Collections.Generic;
using NHNHackathon.AI;
using NHNHackathon.Characters;
using NHNHackathon.Interaction;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(DanceSyncJudge))]
    public sealed class DanceZoneUnlockGate : MonoBehaviour
    {
        [Header("Required Dances")]
        [SerializeField, Tooltip("When enabled, every Dance ID used by this zone's Dance AI must be unlocked.")]
        private bool useDanceAISequence = true;
        [SerializeField, Tooltip("Optional manual requirements. These are checked in addition to the AI sequence.")]
        private List<int> additionalRequiredDanceIds = new();

        [Header("Blocked Feedback")]
        [SerializeField] private string lockedMessage =
            "아직 춤을 배우지 못한거같다 다시오자";
        [SerializeField, Min(0.1f)] private float messageDuration = 1.8f;
        [SerializeField, Min(0.05f)] private float messageCooldown = 1f;

        [Header("Locked Barrier")]
        [SerializeField, Tooltip("Non-trigger collider that physically blocks an uncleared zone.")]
        private BoxCollider lockedBarrier;
        [SerializeField, Min(0.01f), Tooltip("Keeps the trigger slightly larger than the barrier so feedback fires before collision.")]
        private float barrierInset = 0.15f;
        [SerializeField, Min(0f)] private float firstPersonTransitionDuration = 0.55f;

        private Collider zoneCollider;
        private DanceSyncJudge syncJudge;
        private readonly Dictionary<PlayerDanceInput, float> nextMessageTimes = new();

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            syncJudge = GetComponent<DanceSyncJudge>();
            ConfigureLockedBarrier();
        }

        public bool TryAllowEntry(PlayerDanceInput player)
        {
            if (player == null)
            {
                return true;
            }

            bool isAllowed = IsEntryAllowed(player);
            SetBarrierLocked(!isAllowed);
            if (isAllowed)
            {
                return true;
            }

            SwitchToFirstPerson(player.transform);
            ShowLockedMessage(player);
            return false;
        }

        public bool IsEntryAllowed(PlayerDanceInput player)
        {
            PlayerDanceUnlockController unlocks =
                player.GetComponent<PlayerDanceUnlockController>();
            if (unlocks == null)
            {
                return false;
            }

            if (useDanceAISequence)
            {
                DanceSequenceController danceAI = syncJudge != null ? syncJudge.DanceAI : null;
                if (danceAI != null)
                {
                    foreach (int danceId in danceAI.DanceSequence)
                    {
                        if (danceId > 0 && !unlocks.IsUnlocked(danceId))
                        {
                            return false;
                        }
                    }
                }
            }

            foreach (int danceId in additionalRequiredDanceIds)
            {
                if (danceId > 0 && !unlocks.IsUnlocked(danceId))
                {
                    return false;
                }
            }
            return true;
        }

        private void SwitchToFirstPerson(Transform player)
        {
            if (player == null)
            {
                return;
            }

            PlayerCameraController cameraController =
                player.GetComponent<PlayerCameraController>();
            if (cameraController != null)
            {
                cameraController.RequestPerspective(
                    CameraPerspective.FirstPerson, firstPersonTransitionDuration);
            }
        }

        private void ConfigureLockedBarrier()
        {
            if (zoneCollider is not BoxCollider trigger)
            {
                return;
            }

            if (lockedBarrier == null)
            {
                Transform existing = transform.Find("LockedDanceBarrier");
                GameObject barrierObject = existing != null
                    ? existing.gameObject
                    : new GameObject("LockedDanceBarrier");
                barrierObject.transform.SetParent(transform, false);
                barrierObject.layer = gameObject.layer;
                lockedBarrier = barrierObject.GetComponent<BoxCollider>()
                    ?? barrierObject.AddComponent<BoxCollider>();
            }

            lockedBarrier.isTrigger = false;
            lockedBarrier.center = trigger.center;
            //lockedBarrier.size = new Vector3(
            //    Mathf.Max(0.05f, trigger.size.x - barrierInset * 2f),
            //    trigger.size.y,
            //    Mathf.Max(0.05f, trigger.size.z - barrierInset * 2f));
            lockedBarrier.enabled = true;
        }

        private void SetBarrierLocked(bool isLocked)
        {
            if (lockedBarrier != null)
            {
                lockedBarrier.enabled = isLocked;
            }
        }

        private void ShowLockedMessage(PlayerDanceInput player)
        {
            nextMessageTimes.TryGetValue(player, out float nextMessageTime);
            if (Time.unscaledTime < nextMessageTime)
            {
                return;
            }

            nextMessageTimes[player] = Time.unscaledTime + messageCooldown;
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            if (interactor != null && !string.IsNullOrWhiteSpace(lockedMessage))
            {
                interactor.ShowTemporaryMessage(lockedMessage, messageDuration);
            }
        }

        private void OnValidate()
        {
            messageDuration = Mathf.Max(0.1f, messageDuration);
            messageCooldown = Mathf.Max(0.1f, messageCooldown);
            barrierInset = Mathf.Max(0.01f, barrierInset);
            firstPersonTransitionDuration = Mathf.Max(0f, firstPersonTransitionDuration);
        }
    }
}
