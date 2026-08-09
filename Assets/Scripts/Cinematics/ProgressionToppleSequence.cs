using System;
using System.Collections.Generic;
using DG.Tweening;
using NHNHackathon.Inspection;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Cinematics
{
    [Serializable]
    public sealed class ToppleTarget
    {
        [SerializeField] private Transform target;
        [SerializeField, Tooltip("Optional saved world pose. When assigned, the target moves and rotates exactly to this Transform.")]
        private Transform fallenPose;
        [SerializeField, Tooltip("X/Z values choose the fall direction and final angle.")]
        private Vector3 fallEulerAngles = new(85f, 0f, 0f);
        [SerializeField, Min(0f)] private float delay;
        [SerializeField, Min(0.01f)] private float duration = 0.55f;

        public Transform Target => target;
        public Transform FallenPose => fallenPose;
        public Vector3 FallEulerAngles => fallEulerAngles;
        public float Delay => delay;
        public float Duration => duration;
    }

    [DisallowMultipleComponent]
    public sealed class ProgressionToppleSequence : MonoBehaviour
    {
        [Header("Condition")]
        [SerializeField] private ProgressionCondition condition;
        [SerializeField, Tooltip("The sequence begins after this pickup's inspection is closed.")]
        private ItemDefinition triggerItem;
        [SerializeField] private bool waitForInspectionClose = true;
        [SerializeField] private bool oneShot = true;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip toppleSfx;

        [Header("Topple Targets")]
        [SerializeField] private List<ToppleTarget> targets = new();

        [Header("Floor Protection")]
        [SerializeField, Min(0f), Tooltip("Keeps the calculated rotation pivot slightly above the floor.")]
        private float floorClearance = 0.015f;

        private readonly List<Transform> runtimePivots = new();
        private GameProgressionController progressionController;
        private ItemInspectionController inspectionController;
        private Sequence activeSequence;
        private bool hasPlayed;

        private void Start()
        {
            progressionController = GameProgressionController.Instance;
            if (progressionController != null)
            {
                progressionController.ProgressionChanged += Evaluate;
            }

            inspectionController = ItemInspectionController.Instance;
            if (inspectionController == null)
            {
                inspectionController = FindFirstObjectByType<ItemInspectionController>(
                    FindObjectsInactive.Include);
            }
            if (inspectionController != null)
            {
                inspectionController.InspectionClosed += HandleInspectionClosed;
            }

            Evaluate();
        }

        private void Evaluate()
        {
            if (CanPlay() && !waitForInspectionClose)
            {
                PlayToppleSequence();
            }
        }

        private void HandleInspectionClosed(ItemDefinition closedItem)
        {
            if (waitForInspectionClose && closedItem != null
                && closedItem == triggerItem && CanPlay())
            {
                PlayToppleSequence();
            }
        }

        private bool CanPlay()
        {
            return progressionController != null
                && condition != null
                && progressionController.IsCompleted(condition)
                && (!hasPlayed || !oneShot);
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
            activeSequence.InsertCallback(0f, PlayToppleSfx);

            foreach (ToppleTarget entry in targets)
            {
                Tween fallTween = CreateFloorSafeFallTween(entry);
                if (fallTween != null)
                {
                    activeSequence.Insert(entry.Delay, fallTween);
                }
            }
        }

        private Tween CreateFloorSafeFallTween(ToppleTarget entry)
        {
            Transform target = entry?.Target;
            if (target == null)
            {
                return null;
            }

            target.DOKill();
            foreach (Animator animator in target.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            if (entry.FallenPose != null)
            {
                Sequence savedPoseSequence = DOTween.Sequence();
                savedPoseSequence.Join(target.DOMove(
                    entry.FallenPose.position, entry.Duration));
                savedPoseSequence.Join(target.DORotateQuaternion(
                    entry.FallenPose.rotation, entry.Duration));
                return savedPoseSequence
                    .SetEase(Ease.InQuad)
                    .SetLink(target.gameObject);
            }

            if (!TryGetWorldBounds(target, out Bounds bounds))
            {
                return null;
            }

            Vector3 localAxis = new(
                entry.FallEulerAngles.x, 0f, entry.FallEulerAngles.z);
            if (localAxis.sqrMagnitude < 0.001f)
            {
                localAxis = Vector3.right;
            }

            Vector3 worldAxis = target.TransformDirection(localAxis.normalized);
            worldAxis.y = 0f;
            worldAxis = worldAxis.sqrMagnitude < 0.001f
                ? Vector3.right
                : worldAxis.normalized;
            Vector3 fallDirection = Vector3.Cross(worldAxis, Vector3.up).normalized;
            float horizontalRadius = Mathf.Abs(fallDirection.x) * bounds.extents.x
                + Mathf.Abs(fallDirection.z) * bounds.extents.z;
            Vector3 pivotPosition = new(
                bounds.center.x + fallDirection.x * horizontalRadius,
                bounds.min.y + floorClearance,
                bounds.center.z + fallDirection.z * horizontalRadius);

            GameObject pivotObject = new($"{target.name}_TopplePivot");
            Transform pivot = pivotObject.transform;
            pivot.SetPositionAndRotation(pivotPosition, Quaternion.identity);
            runtimePivots.Add(pivot);
            target.SetParent(pivot, true);

            float fallAngle = Mathf.Clamp(
                new Vector2(entry.FallEulerAngles.x, entry.FallEulerAngles.z).magnitude,
                0f, 90f);
            Quaternion fallenRotation = Quaternion.AngleAxis(fallAngle, worldAxis);
            return pivot.DORotateQuaternion(fallenRotation, entry.Duration)
                .SetEase(Ease.InQuad)
                .SetLink(pivotObject);
        }

        private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return true;
        }

        private void PlayToppleSfx()
        {
            if (sfxSource != null && toppleSfx != null)
            {
                sfxSource.PlayOneShot(toppleSfx);
            }
        }

        private void OnDestroy()
        {
            if (progressionController != null)
            {
                progressionController.ProgressionChanged -= Evaluate;
            }
            if (inspectionController != null)
            {
                inspectionController.InspectionClosed -= HandleInspectionClosed;
            }
            activeSequence?.Kill();
        }

        private void OnValidate()
        {
            floorClearance = Mathf.Max(0f, floorClearance);
        }
    }
}
