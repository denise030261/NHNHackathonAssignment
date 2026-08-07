using NHNHackathon.Characters;
using UnityEngine;
using System.Collections.Generic;

namespace NHNHackathon.Cinematics
{
    public enum DoorCrossingAxis
    {
        Right,
        Forward
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DoorPassageCutsceneTrigger : MonoBehaviour
    {
        [SerializeField] private Transform crossingAxis;
        [SerializeField, Tooltip("Local axis that points through the doorway.")]
        private DoorCrossingAxis crossingDirection = DoorCrossingAxis.Right;
        [SerializeField] private ZoneWatcherCaptureCutscene cutscene;
        [SerializeField] private bool oneShot = false;

        private Transform trackedPlayer;
        private float entrySide;
        private bool played;
        private readonly HashSet<Collider> playerColliders = new();

        private void OnTriggerEnter(Collider other)
        {
            if (played && oneShot) return;
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null) return;
            if (trackedPlayer != null && trackedPlayer != player.transform) return;
            if (!playerColliders.Add(other)) return;
            if (playerColliders.Count > 1) return;
            trackedPlayer = player.transform;
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || player.transform != trackedPlayer) return;
            playerColliders.Remove(other);
            if (playerColliders.Count > 0) return;
            trackedPlayer = null;
            if (cutscene == null || !cutscene.TryPlay()) return;
            played = true;
        }

        private void OnValidate()
        {
            Collider value = GetComponent<Collider>();
            if (value != null) value.isTrigger = true;
        }
    }
}
