using System;
using Unity.AI.Navigation;
using UnityEngine;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class DoorNavigationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExitDoor door;
        [SerializeField, Tooltip("Link spanning the two baked NavMesh islands on either side of the door.")]
        private NavMeshLink navigationLink;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges;

        public static event Action NavigationChanged;
        public bool IsPassageAvailable =>
            navigationLink != null && navigationLink.activated;

        private void Awake()
        {
            ResolveReferences();
            SetPassageAvailable(door != null && door.IsOpen, false);
        }

        public void HandleDoorOpening()
        {
            // Keep the route blocked while the panel is still crossing the doorway.
            SetPassageAvailable(false);
        }

        public void HandleDoorOpened()
        {
            SetPassageAvailable(true);
        }

        public void HandleDoorClosing()
        {
            // Stop new paths before the closing animation begins.
            SetPassageAvailable(false);
        }

        public void HandleDoorClosed()
        {
            SetPassageAvailable(false);
        }

        public void SetPassageAvailable(bool available, bool notify = true)
        {
            if (navigationLink == null || navigationLink.activated == available)
            {
                return;
            }

            navigationLink.activated = available;
            if (logStateChanges)
            {
                Debug.Log($"{name}: Door NavMeshLink {(available ? "opened" : "closed")}", this);
            }
            if (notify)
            {
                NavigationChanged?.Invoke();
            }
        }

        private void ResolveReferences()
        {
            door ??= GetComponent<ExitDoor>();
            navigationLink ??= GetComponentInChildren<NavMeshLink>(true);
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
