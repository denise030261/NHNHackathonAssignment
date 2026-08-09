using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    public sealed class PlayerDanceInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private DanceInputMapping inputMapping;
        [SerializeField, Tooltip("When assigned, only dances unlocked by reading papers can be performed.")]
        private PlayerDanceUnlockController unlockController;

        public event Action<int, float> DanceInputPerformed;
        public bool IsDanceModifierHeld => inputMapping != null
            && UnityEngine.Input.GetKey(inputMapping.ModifierKey);

        private void Update()
        {
            if (inputMapping == null)
            {
                return;
            }

            bool modifierHeld = UnityEngine.Input.GetKey(inputMapping.ModifierKey);
            if (!modifierHeld)
            {
                return;
            }

            bool modifierPressed = UnityEngine.Input.GetKeyDown(inputMapping.ModifierKey);

            foreach (DanceInputBinding binding in inputMapping.Bindings)
            {
                if (binding != null
                    && (UnityEngine.Input.GetKeyDown(binding.Key)
                        || (modifierPressed && UnityEngine.Input.GetKey(binding.Key))))
                {
                    PerformDance(binding.DanceId);
                    break;
                }
            }
        }

        private void PerformDance(int danceId)
        {
            if (unlockController != null && !unlockController.IsUnlocked(danceId))
            {
                unlockController.NotifyLockedDance(danceId);
                return;
            }

            DanceInputPerformed?.Invoke(danceId, Time.time);
        }

        private void Reset()
        {
            unlockController = GetComponent<PlayerDanceUnlockController>();
        }

        private void Awake()
        {
            if (unlockController == null)
            {
                unlockController = GetComponent<PlayerDanceUnlockController>();
            }
        }
    }
}
