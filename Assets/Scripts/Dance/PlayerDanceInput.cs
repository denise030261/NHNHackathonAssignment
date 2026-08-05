using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    public sealed class PlayerDanceInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private DanceInputMapping inputMapping;

        public event Action<int, float> DanceInputPerformed;

        private void Update()
        {
            if (inputMapping == null)
            {
                return;
            }

            foreach (DanceInputBinding binding in inputMapping.Bindings)
            {
                if (binding != null && UnityEngine.Input.GetKeyDown(binding.Key))
                {
                    PerformDance(binding.DanceId);
                    break;
                }
            }
        }

        private void PerformDance(int danceId)
        {
            DanceInputPerformed?.Invoke(danceId, Time.time);
        }
    }
}
