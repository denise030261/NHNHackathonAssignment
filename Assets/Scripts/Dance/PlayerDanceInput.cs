using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceColorVisualizer))]
    public sealed class PlayerDanceInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private DanceInputMapping inputMapping;

        [Header("Visual Feedback")]
        [SerializeField, Min(0f), Tooltip("Seconds the player's temporary dance colour remains visible.")]
        private float displayDuration = 0.5f;

        private DanceColorVisualizer colorVisualizer;

        public event Action<int, float> DanceInputPerformed;

        private void Awake()
        {
            colorVisualizer = GetComponent<DanceColorVisualizer>();
        }

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
            colorVisualizer.ShowDance(danceId, displayDuration);
            DanceInputPerformed?.Invoke(danceId, Time.time);
        }
    }
}
