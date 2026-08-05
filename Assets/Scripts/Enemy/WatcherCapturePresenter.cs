using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WatcherCapturePresenter : MonoBehaviour
    {
        [Header("Model References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform upperBodyBone;
        [SerializeField] private Transform lookTarget;

        [Header("Capture Motion")]
        [SerializeField, Min(0f)] private float upperBodyApproachDistance = 0.3f;
        [SerializeField, Min(0.01f)] private float approachDuration = 0.45f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.75f;
        [SerializeField] private Vector3 shakeStrength = new(7f, 12f, 5f);
        [SerializeField, Min(1)] private int shakeVibrato = 20;

        private NavMeshAgent agent;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private bool poseStored;
        private Tween captureTween;

        public Transform LookTarget => lookTarget != null ? lookTarget : upperBodyBone;

        private void Awake()
        {
            ResolveReferences();
        }

        public Tween CreateCaptureTween(Transform playerCamera)
        {
            ResolveReferences();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            if (animator != null) animator.speed = 0f;
            if (upperBodyBone == null) return DOVirtual.DelayedCall(0f, () => { });

            originalLocalPosition = upperBodyBone.localPosition;
            originalLocalRotation = upperBodyBone.localRotation;
            poseStored = true;
            Vector3 direction = playerCamera != null
                ? (playerCamera.position - upperBodyBone.position).normalized
                : transform.forward;

            Sequence motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            motion.Append(upperBodyBone.DOMove(
                    upperBodyBone.position + direction * upperBodyApproachDistance,
                    approachDuration)
                .SetEase(Ease.InQuad));
            motion.Append(upperBodyBone.DOShakeRotation(
                shakeDuration, shakeStrength, shakeVibrato, 65f, true));
            captureTween = motion;
            return motion;
        }

        private void ResolveReferences()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman) return;
            if (upperBodyBone == null)
            {
                upperBodyBone = animator.GetBoneTransform(HumanBodyBones.UpperChest)
                    ?? animator.GetBoneTransform(HumanBodyBones.Chest)
                    ?? animator.GetBoneTransform(HumanBodyBones.Spine);
            }
            if (lookTarget == null)
            {
                lookTarget = animator.GetBoneTransform(HumanBodyBones.Head) ?? upperBodyBone;
            }
        }

        private void OnDisable()
        {
            captureTween?.Kill();
            if (poseStored && upperBodyBone != null)
            {
                upperBodyBone.localPosition = originalLocalPosition;
                upperBodyBone.localRotation = originalLocalRotation;
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
