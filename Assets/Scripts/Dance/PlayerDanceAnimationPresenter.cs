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
        }

        private void HandleDanceInput(int danceId, float inputTime)
        {
            if (animator == null) return;
            foreach (DanceAnimationMapping mapping in danceAnimations)
            {
                if (mapping == null || mapping.DanceId != danceId
                    || mapping.AnimationClip == null) continue;

                string stateName = $"Dance{danceId}";
                int stateHash = Animator.StringToHash(stateName);
                if (!animator.HasState(0, stateHash))
                {
                    Debug.LogWarning(
                        $"Dance state '{stateName}' does not exist on '{animator.name}'.",
                        animator);
                    return;
                }

                animator.speed = mapping.PlaybackSpeed;
                animator.CrossFadeInFixedTime(
                    stateHash, transitionDuration, 0, 0f);
                return;
            }
        }

        private void OnValidate()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }
    }
}
