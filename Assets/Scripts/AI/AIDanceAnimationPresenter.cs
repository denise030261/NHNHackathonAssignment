using System;
using System.Collections.Generic;
using NHNHackathon.Dance;
using UnityEngine;

namespace NHNHackathon.AI
{
    [Serializable]
    public sealed class DanceAnimationMapping
    {
        [SerializeField] private int danceId = 1;
        [SerializeField] private string animatorStateName;

        public int DanceId => danceId;
        public string AnimatorStateName => animatorStateName;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceSequenceController))]
    public sealed class AIDanceAnimationPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Dance Animation Mapping")]
        [SerializeField, Tooltip("Maps each editable Dance ID to an Animator state.")]
        private List<DanceAnimationMapping> danceAnimations = new();
        [SerializeField, Min(0f)] private float transitionDuration = 0.08f;

        private DanceSequenceController sequenceController;

        private void Awake()
        {
            sequenceController = GetComponent<DanceSequenceController>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void OnEnable()
        {
            sequenceController ??= GetComponent<DanceSequenceController>();
            sequenceController.DanceStepChanged += HandleDanceStepChanged;
        }

        private void OnDisable()
        {
            if (sequenceController != null)
            {
                sequenceController.DanceStepChanged -= HandleDanceStepChanged;
            }
        }

        private void HandleDanceStepChanged(DanceDefinition dance, int stepIndex, float beatTime)
        {
            if (animator == null || dance == null)
            {
                return;
            }

            foreach (DanceAnimationMapping mapping in danceAnimations)
            {
                if (mapping != null && mapping.DanceId == dance.Id
                    && !string.IsNullOrWhiteSpace(mapping.AnimatorStateName))
                {
                    animator.CrossFadeInFixedTime(
                        mapping.AnimatorStateName, transitionDuration, 0, 0f);
                    return;
                }
            }
        }

        private void OnValidate()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }
    }
}
