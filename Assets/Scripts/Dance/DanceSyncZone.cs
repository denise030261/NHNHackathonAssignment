using System.Collections.Generic;
using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(DanceSyncJudge))]
    public sealed class DanceSyncZone : MonoBehaviour
    {
        [Header("Scene Visualization")]
        [SerializeField] private Color inactiveColor = new Color(1f, 0.2f, 0.2f, 0.2f);
        [SerializeField] private Color successColor = new Color(0.2f, 1f, 0.3f, 0.25f);

        private readonly Dictionary<PlayerDanceInput, int> playerColliderCounts =
            new Dictionary<PlayerDanceInput, int>();
        private DanceSyncJudge syncJudge;
        private DanceZoneUnlockGate unlockGate;
        private Collider zoneCollider;

        public event Action<PlayerDanceInput> PlayerEntered;
        public event Action<PlayerDanceInput> PlayerExited;

        private void Awake()
        {
            syncJudge = GetComponent<DanceSyncJudge>();
            unlockGate = GetComponent<DanceZoneUnlockGate>();
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerDanceInput player = other.GetComponentInParent<PlayerDanceInput>();
            if (player == null)
            {
                return;
            }
            if (unlockGate != null && !unlockGate.TryAllowEntry(player))
            {
                return;
            }

            playerColliderCounts.TryGetValue(player, out int count);
            playerColliderCounts[player] = count + 1;
            if (count == 0)
            {
                syncJudge.SetActivePlayer(player);
                PlayerEntered?.Invoke(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerDanceInput player = other.GetComponentInParent<PlayerDanceInput>();
            if (player == null || !playerColliderCounts.TryGetValue(player, out int count))
            {
                return;
            }

            if (count > 1)
            {
                playerColliderCounts[player] = count - 1;
                return;
            }

            playerColliderCounts.Remove(player);
            syncJudge.SetActivePlayer(null);
            PlayerExited?.Invoke(player);
        }

        private void OnDrawGizmos()
        {
            DanceSyncJudge judge = syncJudge != null ? syncJudge : GetComponent<DanceSyncJudge>();
            Collider collider = zoneCollider != null ? zoneCollider : GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            Gizmos.color = judge != null && judge.IsBlendingIn ? successColor : inactiveColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (collider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
