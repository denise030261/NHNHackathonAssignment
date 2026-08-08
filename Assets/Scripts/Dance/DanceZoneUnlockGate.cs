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

        [Header("Push Back")]
        [SerializeField, Min(0.05f), Tooltip("Extra distance placed outside the zone boundary.")]
        private float boundaryPadding = 0.75f;
        [SerializeField, Min(0f)] private float turnAroundDuration = 0.55f;
        [SerializeField, Min(0f)] private float firstPersonTransitionDuration = 0.55f;

        private Collider zoneCollider;
        private DanceSyncJudge syncJudge;
        private readonly Dictionary<PlayerDanceInput, float> nextMessageTimes = new();

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            syncJudge = GetComponent<DanceSyncJudge>();
        }

        public bool TryAllowEntry(PlayerDanceInput player)
        {
            if (player == null || IsEntryAllowed(player))
            {
                return true;
            }

            MoveOutsideZone(player.transform);
            TurnPlayerAwayFromZone(player.transform);
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

        private void TurnPlayerAwayFromZone(Transform player)
        {
            if (player == null)
            {
                return;
            }

            Vector3 awayDirection = player.position - zoneCollider.bounds.center;
            awayDirection.y = 0f;
            if (awayDirection.sqrMagnitude <= 0.0001f)
            {
                awayDirection = -player.forward;
            }

            PlayerCameraController cameraController =
                player.GetComponent<PlayerCameraController>();
            if (cameraController == null)
            {
                player.rotation = Quaternion.LookRotation(awayDirection.normalized, Vector3.up);
                return;
            }

            cameraController.RequestPerspective(
                CameraPerspective.FirstPerson, firstPersonTransitionDuration);
            cameraController.RequestFacingDirection(awayDirection, turnAroundDuration);
        }

        private void MoveOutsideZone(Transform player)
        {
            if (player == null || zoneCollider == null)
            {
                return;
            }

            Vector3 destination = zoneCollider is BoxCollider box
                ? FindNearestBoxExit(box, player.position)
                : FindBoundsExit(zoneCollider.bounds, player.position);

            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null)
            {
                controller.enabled = false;
            }
            player.position = destination;
            if (controller != null)
            {
                controller.enabled = wasEnabled;
            }
        }

        private Vector3 FindNearestBoxExit(BoxCollider box, Vector3 playerPosition)
        {
            Vector3 local = box.transform.InverseTransformPoint(playerPosition) - box.center;
            Vector3 extents = box.size * 0.5f;
            float distanceToX = extents.x - Mathf.Abs(local.x);
            float distanceToZ = extents.z - Mathf.Abs(local.z);

            if (distanceToX <= distanceToZ)
            {
                float sign = Mathf.Approximately(local.x, 0f) ? 1f : Mathf.Sign(local.x);
                local.x = sign * (extents.x + boundaryPadding);
            }
            else
            {
                float sign = Mathf.Approximately(local.z, 0f) ? 1f : Mathf.Sign(local.z);
                local.z = sign * (extents.z + boundaryPadding);
            }

            Vector3 destination = box.transform.TransformPoint(box.center + local);
            destination.y = playerPosition.y;
            return destination;
        }

        private Vector3 FindBoundsExit(Bounds bounds, Vector3 playerPosition)
        {
            Vector3 direction = playerPosition - bounds.center;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }
            direction.Normalize();
            float distance = Mathf.Max(bounds.extents.x, bounds.extents.z) + boundaryPadding;
            Vector3 destination = bounds.center + direction * distance;
            destination.y = playerPosition.y;
            return destination;
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
            boundaryPadding = Mathf.Max(0.05f, boundaryPadding);
            turnAroundDuration = Mathf.Max(0f, turnAroundDuration);
            firstPersonTransitionDuration = Mathf.Max(0f, firstPersonTransitionDuration);
        }
    }
}
