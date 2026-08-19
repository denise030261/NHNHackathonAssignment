using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Interaction
{
    public static class InteractableRegistry
    {
        private static readonly HashSet<IInteractable> activeInteractables = new();

        public static IReadOnlyCollection<IInteractable> ActiveInteractables =>
            activeInteractables;

        public static void Register(IInteractable interactable)
        {
            if (interactable != null)
            {
                activeInteractables.Add(interactable);
            }
        }

        public static void Unregister(IInteractable interactable)
        {
            if (interactable != null)
            {
                activeInteractables.Remove(interactable);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            activeInteractables.Clear();
        }
    }
}
