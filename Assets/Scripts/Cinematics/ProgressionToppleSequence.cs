using System;
using System.Collections.Generic;
using DG.Tweening;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Cinematics
{
    [Serializable]
    public sealed class ToppleTarget
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 fallEulerAngles = new(85f, 0f, 0f);
        [SerializeField, Min(0f)] private float delay;
        [SerializeField, Min(0.01f)] private float duration = 0.55f;
        [SerializeField, Min(0f)] private float dropDistance = 0.08f;

        public Transform Target => target;
        public Vector3 FallEulerAngles => fallEulerAngles;
        public float Delay => delay;
        public float Duration => duration;
        public float DropDistance => dropDistance;
    }

    [DisallowMultipleComponent]
    public sealed class ProgressionToppleSequence : MonoBehaviour
    {
        [Header("Condition")]
        [SerializeField] private ProgressionCondition condition;
        [SerializeField] private bool oneShot = true;

        [Header("Topple Targets")]
        [SerializeField] private List<ToppleTarget> targets = new();

        private GameProgressionController progressionController;
        private Sequence activeSequence;
        private bool hasPlayed;

        private void Start()
        {
            progressionController = GameProgressionController.Instance;
            if (progressionController != null)
            {
                progressionController.ProgressionChanged += Evaluate;
            }
            Evaluate();
        }

        private void Evaluate()
        {
            if (progressionController == null || condition == null
                || !progressionController.IsCompleted(condition)
                || hasPlayed && oneShot)
            {
                return;
            }

            PlayToppleSequence();
        }

        [ContextMenu("Preview Topple Sequence")]
        public void PlayToppleSequence()
        {
            if (hasPlayed && oneShot)
            {
                return;
            }

            hasPlayed = true;
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence().SetLink(gameObject);

            // TODO(Audio): 쓰러지는 SFX 리소스가 준비되면 연출 시작 시 여기서 재생한다.
            // toppleAudioSource.PlayOneShot(toppleSfx);
            foreach (ToppleTarget entry in targets)
            {
                if (entry?.Target == null)
                {
                    continue;
                }

                Transform target = entry.Target;
                Quaternion fallenRotation = target.localRotation
                    * Quaternion.Euler(entry.FallEulerAngles);
                activeSequence.Insert(entry.Delay,
                    target.DOLocalRotateQuaternion(fallenRotation, entry.Duration)
                        .SetEase(Ease.InQuad));
                activeSequence.Insert(entry.Delay,
                    target.DOLocalMove(
                            target.localPosition + Vector3.down * entry.DropDistance,
                            entry.Duration)
                        .SetEase(Ease.InQuad));
            }
        }

        private void OnDestroy()
        {
            if (progressionController != null)
            {
                progressionController.ProgressionChanged -= Evaluate;
            }
            activeSequence?.Kill();
        }
    }
}
