using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerDanceInput))]
    public sealed class PlayerDanceAnimationPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Dance Animation Mapping")]
        [SerializeField] private List<DanceAnimationMapping> danceAnimations = new();
        [SerializeField, Min(0f)] private float transitionDuration = 0.2f;

        private PlayerDanceInput danceInput;
        private int currentDanceId = -1;

        private void Awake()
        {
            danceInput = GetComponent<PlayerDanceInput>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            danceInput ??= GetComponent<PlayerDanceInput>();
            danceInput.DanceInputPerformed += HandleDanceInput;
        }

        private void OnDisable()
        {
            if (danceInput != null) danceInput.DanceInputPerformed -= HandleDanceInput;
            currentDanceId = -1;
        }

        private void HandleDanceInput(int danceId, float inputTime)
        {
            if (animator == null) return;
            foreach (DanceAnimationMapping mapping in danceAnimations)
            {
                if (mapping == null || mapping.DanceId != danceId
                    || mapping.AnimationClip == null) continue;

                animator.speed = mapping.PlaybackSpeed;
                if (currentDanceId == danceId)
                {
                    animator.Play(mapping.AnimationClip.name, 0, 0f);
                    animator.Update(0f);
                }
                else
                {
                    animator.CrossFadeInFixedTime(
                        mapping.AnimationClip.name, transitionDuration, 0, 0f);
                }
                currentDanceId = danceId;
                return;
            }
        }

        private void OnValidate()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }
    }
}
