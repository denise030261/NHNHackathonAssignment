using System.Collections.Generic;
using NHNHackathon.Dance;
using UnityEngine;

namespace NHNHackathon.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceSequenceController))]
    public sealed class AIDanceAnimationPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Dance Animation Mapping")]
        [SerializeField, Tooltip("Maps each editable Dance ID to an Animator state.")]
        private List<DanceAnimationMapping> danceAnimations = new();
        [SerializeField, Min(0f), Tooltip("Seconds used to blend from the current dance into the next dance.")]
        private float transitionDuration = 0.2f;

        [Header("Dance SFX")]
        [SerializeField] private AudioSource danceSfxSource;
        [SerializeField] private AudioClip danceSfx;
        [SerializeField, Range(0f, 1f)] private float danceSfxVolumeScale = 1f;
        private DanceSequenceController sequenceController;
        private int currentDanceId = -1;

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
            currentDanceId = -1;
        }

        private void HandleDanceStepChanged(DanceDefinition dance, int stepIndex, float beatTime)
        {
            if (danceSfxSource != null && danceSfx != null)
            {
                danceSfxSource.PlayOneShot(danceSfx, danceSfxVolumeScale);
            }

            if (animator == null || dance == null || dance.Id == currentDanceId)
            {
                return;
            }

            foreach (DanceAnimationMapping mapping in danceAnimations)
            {
                if (mapping != null && mapping.DanceId == dance.Id
                    && mapping.AnimationClip != null)
                {
                    currentDanceId = dance.Id;
                    animator.speed = mapping.PlaybackSpeed;
                    animator.CrossFadeInFixedTime(
                        mapping.AnimationClip.name, transitionDuration, 0, 0f);
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
