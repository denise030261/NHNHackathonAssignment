using System;
using System.Collections.Generic;
using NHNHackathon.Dance;
using UnityEngine;

namespace NHNHackathon.AI
{
    [DisallowMultipleComponent]
    public sealed class DanceSequenceController : MonoBehaviour
    {
        [Header("Shared Dance Data")]
        [SerializeField] private DanceCatalog danceCatalog;

        [Header("Rhythm")]
        [SerializeField, Min(0.01f), Tooltip("Seconds between consecutive dance steps.")]
        private float beatInterval = 1f;

        [Header("Dance Sequence")]
        [SerializeField, Tooltip("Played from top to bottom, then repeated from the first entry.")]
        private List<int> danceSequence = new List<int>();

        private int currentStepIndex = -1;
        private float nextBeatTime;
        private bool warnedAboutEmptySequence;

        public event Action<DanceDefinition, int, float> DanceStepChanged;

        public int CurrentStepIndex => currentStepIndex;
        public IReadOnlyList<int> DanceSequence => danceSequence;
        public float BeatInterval => beatInterval;
        public float NextBeatTime => nextBeatTime;

        private void OnEnable()
        {
            currentStepIndex = -1;
            nextBeatTime = Time.time;
            warnedAboutEmptySequence = false;
        }

        private void Update()
        {
            if (danceSequence == null || danceSequence.Count == 0)
            {
                WarnAboutEmptySequenceOnce();
                currentStepIndex = -1;
                return;
            }

            if (currentStepIndex >= danceSequence.Count)
            {
                currentStepIndex = -1;
            }

            if (Time.time < nextBeatTime)
            {
                return;
            }

            AdvanceToNextStep();
            nextBeatTime += beatInterval;

            if (nextBeatTime <= Time.time)
            {
                nextBeatTime = Time.time + beatInterval;
            }
        }

        public bool TryGetCurrentDance(out DanceDefinition dance)
        {
            dance = null;
            bool hasCurrentStep = danceSequence != null
                && currentStepIndex >= 0
                && currentStepIndex < danceSequence.Count;
            return hasCurrentStep
                && danceCatalog != null
                && danceCatalog.TryGetDance(danceSequence[currentStepIndex], out dance);
        }

        public bool TryGetNextDance(out DanceDefinition dance)
        {
            dance = null;
            if (danceSequence == null || danceSequence.Count == 0 || danceCatalog == null)
            {
                return false;
            }

            int nextIndex = (currentStepIndex + 1) % danceSequence.Count;
            return danceCatalog.TryGetDance(danceSequence[nextIndex], out dance);
        }

        private void AdvanceToNextStep()
        {
            currentStepIndex = (currentStepIndex + 1) % danceSequence.Count;
            int danceId = danceSequence[currentStepIndex];
            if (danceCatalog == null || !danceCatalog.TryGetDance(danceId, out DanceDefinition currentDance))
            {
                Debug.LogWarning($"Dance ID {danceId} on {name} is missing from the catalog.", this);
                return;
            }

            DanceStepChanged?.Invoke(currentDance, currentStepIndex, Time.time);
        }

        private void WarnAboutEmptySequenceOnce()
        {
            if (warnedAboutEmptySequence)
            {
                return;
            }

            Debug.LogWarning($"{name} cannot dance because its Dance Sequence is empty.", this);
            warnedAboutEmptySequence = true;
        }

        private void OnValidate()
        {
            beatInterval = Mathf.Max(0.01f, beatInterval);
        }
    }
}
