using System.Collections.Generic;
using DG.Tweening;
using NHNHackathon.Characters;
using NHNHackathon.Enemy;
using NHNHackathon.LightSystem;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.Cinematics
{
    [DisallowMultipleComponent]
    public sealed class ZoneWatcherCaptureCutscene : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Behaviour playerCameraController;
        [SerializeField] private PlayerFlashlightController playerFlashlight;
        [SerializeField] private Behaviour[] playerControls;

        [Header("Actors")]
        [SerializeField] private GameObject failedDancer;
        [SerializeField] private Animator failedDancerAnimator;
        [SerializeField] private string wrongDanceStateName = "Dance4";
        [SerializeField] private EnemyController watcherController;
        [SerializeField] private NavMeshAgent watcherAgent;
        [SerializeField] private Animator watcherAnimator;

        [Header("Watcher Pickup Animation")]
        [SerializeField] private AnimationClip watcherPickupAnimation;
        [SerializeField] private string watcherPickupStateName = "Pickup";
        [SerializeField, Min(0f)] private float pickupBlendDuration = 0.1f;
        [SerializeField, Min(0f)] private float pickupFallbackDuration = 2.966667f;
        [SerializeField] private string watcherReturnStateName = "Idle";
        [SerializeField, Min(0f)] private float returnBlendDuration = 0.1f;

        [Header("Failed Dancer Hand Attachment")]
        [SerializeField] private Transform watcherHand;
        [SerializeField] private string watcherHandPath =
            "Visual/WatcherRig/spine/spine.001/spine.002/spine.003/shoulder.R/upper_arm.R/forearm.R/hand.R";
        [SerializeField] private Vector3 dancerHandLocalPosition;
        [SerializeField] private Vector3 dancerHandLocalEulerAngles;

        [Header("Scene Points")]
        [SerializeField] private Transform cutsceneCameraPoint;
        [SerializeField] private Transform cameraLookTarget;
        [SerializeField, Tooltip("Camera waypoints followed before reaching the cutscene camera point.")]
        private Transform[] cameraRoute;
        [SerializeField] private Transform watcherCapturePoint;
        [SerializeField] private Transform[] corridorRoute;

        [Header("Camera Route")]
        [SerializeField, Min(0.01f)] private float cameraRouteSegmentDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.18f;
        [SerializeField] private LayerMask cameraCollisionMask = ~(1 << 2);

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float cameraMoveDuration = 1f;
        [SerializeField, Min(0f)] private float normalDanceDuration = 1.5f;
        [SerializeField, Min(0f)] private float wrongDanceDuration = 1f;
        [SerializeField, Min(0.01f)] private float watcherApproachDuration = 1.2f;
        [SerializeField, Min(0f)] private float captureHoldDuration = 0.5f;
        [SerializeField, Min(0.01f), Tooltip("Time used for the watcher to face each corridor waypoint.")]
        private float watcherTurnDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float corridorSegmentDuration = 1f;
        [SerializeField, Min(0.01f)] private float cameraReturnDuration = 0.9f;

        private readonly List<(Behaviour control, bool enabled)> controlStates = new();
        private Sequence sequence;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private Vector3 watcherStartPosition;
        private Quaternion watcherStartRotation;
        private Transform dancerOriginalParent;
        private Vector3 dancerOriginalLocalPosition;
        private Quaternion dancerOriginalLocalRotation;
        private Vector3 dancerOriginalLocalScale;
        private bool watcherWasEnabled;
        private bool agentWasEnabled;
        private CameraPerspective savedFlashlightPerspective;
        private bool flashlightAttachmentOverridden;
        private bool playing;

        public bool TryPlay()
        {
            if (playing || playerCamera == null || failedDancer == null
                || watcherController == null || cutsceneCameraPoint == null) return false;

            playing = true;
            SaveAndLockPlayer();
            PrepareActors();
            BuildSequence();
            return true;
        }

        private void SaveAndLockPlayer()
        {
            savedCameraPosition = playerCamera.transform.position;
            savedCameraRotation = playerCamera.transform.rotation;
            playerFlashlight ??= FindAnyObjectByType<PlayerFlashlightController>(
                FindObjectsInactive.Include);
            flashlightAttachmentOverridden = playerFlashlight != null
                && playerFlashlight.IsFlashlightEnabled;
            if (flashlightAttachmentOverridden)
            {
                savedFlashlightPerspective = playerFlashlight.AttachmentPerspective;
                playerFlashlight.SetAttachmentPerspective(
                    CameraPerspective.ThirdPerson, true);
            }
            controlStates.Clear();
            AddAndDisable(playerCameraController);
            AddAndDisable(playerFlashlight);
            if (playerControls != null)
                foreach (Behaviour control in playerControls) AddAndDisable(control);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void AddAndDisable(Behaviour control)
        {
            if (control == null || controlStates.Exists(value => value.control == control)) return;
            controlStates.Add((control, control.enabled));
            control.enabled = false;
        }

        private void PrepareActors()
        {
            failedDancer.SetActive(true);
            dancerOriginalParent = failedDancer.transform.parent;
            dancerOriginalLocalPosition = failedDancer.transform.localPosition;
            dancerOriginalLocalRotation = failedDancer.transform.localRotation;
            dancerOriginalLocalScale = failedDancer.transform.localScale;
            watcherStartPosition = watcherController.transform.position;
            watcherStartRotation = watcherController.transform.rotation;
            watcherWasEnabled = watcherController.enabled;
            watcherController.enabled = false;
            agentWasEnabled = watcherAgent != null && watcherAgent.enabled;
            if (watcherAgent != null && watcherAgent.enabled)
            {
                if (watcherAgent.isOnNavMesh) { watcherAgent.isStopped = true; watcherAgent.ResetPath(); }
                watcherAgent.enabled = false;
            }
            if (watcherAnimator != null) watcherAnimator.speed = 1f;
        }

        private void BuildSequence()
        {
            sequence = DOTween.Sequence().SetLink(gameObject);
            AppendCameraRouteForward(sequence);
            Quaternion cameraRotation = LookRotation(cutsceneCameraPoint.position, cameraLookTarget);
            sequence.Append(playerCamera.transform.DOMove(cutsceneCameraPoint.position, cameraMoveDuration).SetEase(Ease.InOutSine));
            sequence.Join(playerCamera.transform.DORotateQuaternion(cameraRotation, cameraMoveDuration).SetEase(Ease.InOutSine));
            sequence.AppendInterval(normalDanceDuration);
            sequence.AppendCallback(PlayWrongDance);
            sequence.AppendInterval(wrongDanceDuration);

            if (watcherCapturePoint != null)
            {
                sequence.Append(watcherController.transform.DOMove(watcherCapturePoint.position, watcherApproachDuration).SetEase(Ease.InOutSine));
                sequence.Join(watcherController.transform.DORotateQuaternion(
                    LookRotation(watcherController.transform.position, failedDancer.transform), watcherApproachDuration));
            }
            sequence.AppendCallback(BeginPickup);
            sequence.AppendInterval(GetPickupAnimationDuration());
            sequence.AppendInterval(captureHoldDuration);
            AppendCorridorMovement(sequence);
            AppendCameraRouteBackward(sequence);
            sequence.Append(playerCamera.transform.DOMove(savedCameraPosition, cameraReturnDuration).SetEase(Ease.InOutSine));
            sequence.Join(playerCamera.transform.DORotateQuaternion(savedCameraRotation, cameraReturnDuration).SetEase(Ease.InOutSine));
            sequence.OnComplete(FinishCutscene);
        }

        private void AppendCameraRouteForward(Sequence target)
        {
            if (cameraRoute == null) return;
            foreach (Transform point in cameraRoute)
            {
                if (point == null) continue;
                target.Append(playerCamera.transform.DOMove(
                    point.position, cameraRouteSegmentDuration).SetEase(Ease.InOutSine));
                target.Join(playerCamera.transform.DORotateQuaternion(
                    LookRotation(point.position, cameraLookTarget), cameraRouteSegmentDuration));
            }
        }

        private void AppendCameraRouteBackward(Sequence target)
        {
            if (cameraRoute == null) return;
            for (int index = cameraRoute.Length - 1; index >= 0; index--)
            {
                Transform point = cameraRoute[index];
                if (point == null) continue;
                target.Append(playerCamera.transform.DOMove(
                    point.position, cameraRouteSegmentDuration).SetEase(Ease.InOutSine));
                target.Join(playerCamera.transform.DORotateQuaternion(
                    LookRotation(point.position, cameraLookTarget), cameraRouteSegmentDuration));
            }
        }

        private void PlayWrongDance()
        {
            if (failedDancerAnimator != null && !string.IsNullOrWhiteSpace(wrongDanceStateName))
                failedDancerAnimator.Play(wrongDanceStateName, 0, 0f);
        }

        private void BeginPickup()
        {
            AttachDancerToWatcherHand();

            if (watcherAnimator == null
                || string.IsNullOrWhiteSpace(watcherPickupStateName))
            {
                return;
            }

            watcherAnimator.speed = 1f;
            watcherAnimator.CrossFadeInFixedTime(
                watcherPickupStateName,
                pickupBlendDuration,
                0,
                0f);
        }

        private float GetPickupAnimationDuration()
        {
            return watcherPickupAnimation != null
                ? watcherPickupAnimation.length
                : pickupFallbackDuration;
        }

        private void AttachDancerToWatcherHand()
        {
            Transform hand = ResolveWatcherHand();
            if (hand == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ZoneWatcherCaptureCutscene)}] Watcher hand was not found at '{watcherHandPath}'.",
                    this);
                return;
            }

            failedDancer.transform.SetParent(hand, true);
            failedDancer.transform.localPosition = dancerHandLocalPosition;
            failedDancer.transform.localRotation = Quaternion.Euler(
                dancerHandLocalEulerAngles);
        }

        private Transform ResolveWatcherHand()
        {
            if (watcherHand != null)
            {
                return watcherHand;
            }

            if (watcherController == null || string.IsNullOrWhiteSpace(watcherHandPath))
            {
                return null;
            }

            watcherHand = watcherController.transform.Find(watcherHandPath);
            return watcherHand;
        }

        private void AppendCorridorMovement(Sequence target)
        {
            if (corridorRoute == null) return;

            Vector3 segmentOrigin = watcherCapturePoint != null
                ? watcherCapturePoint.position
                : watcherController.transform.position;
            bool isFirstSegment = true;

            foreach (Transform point in corridorRoute)
            {
                if (point == null) continue;

                Quaternion targetRotation = LookRotation(segmentOrigin, point);
                if (isFirstSegment)
                {
                    // Finish the initial turn before walking away with the doll.
                    target.Append(watcherController.transform
                        .DORotateQuaternion(targetRotation, watcherTurnDuration)
                        .SetEase(Ease.InOutSine));
                    isFirstSegment = false;
                }

                target.Append(watcherController.transform
                    .DOMove(point.position, corridorSegmentDuration)
                    .SetEase(Ease.InOutSine));
                target.Join(watcherController.transform
                    .DORotateQuaternion(targetRotation, watcherTurnDuration)
                    .SetEase(Ease.InOutSine));

                segmentOrigin = point.position;
            }
        }

        private static Quaternion LookRotation(Vector3 origin, Transform target)
        {
            if (target == null) return Quaternion.identity;
            Vector3 direction = target.position - origin;
            return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : Quaternion.identity;
        }

        private void FinishCutscene()
        {
            sequence = null;
            failedDancer.transform.SetParent(dancerOriginalParent, false);
            failedDancer.transform.localPosition = dancerOriginalLocalPosition;
            failedDancer.transform.localRotation = dancerOriginalLocalRotation;
            failedDancer.transform.localScale = dancerOriginalLocalScale;
            failedDancer.SetActive(false);
            watcherController.transform.SetPositionAndRotation(watcherStartPosition, watcherStartRotation);
            if (watcherAgent != null)
            {
                watcherAgent.enabled = agentWasEnabled;
                if (watcherAgent.enabled
                    && NavMesh.SamplePosition(watcherStartPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    watcherAgent.Warp(hit.position);
                    watcherAgent.isStopped = false;
                }
            }
            watcherController.enabled = watcherWasEnabled;
            if (watcherWasEnabled)
            {
                watcherController.ResumeAfterCutscene();
            }
            if (watcherAnimator != null)
            {
                watcherAnimator.speed = 1f;
                if (!string.IsNullOrWhiteSpace(watcherReturnStateName))
                {
                    watcherAnimator.CrossFadeInFixedTime(
                        watcherReturnStateName,
                        returnBlendDuration,
                        0,
                        0f);
                }
            }
            foreach ((Behaviour control, bool wasEnabled) in controlStates)
                if (control != null) control.enabled = wasEnabled;
            if (flashlightAttachmentOverridden && playerFlashlight != null)
            {
                playerFlashlight.SetAttachmentPerspective(
                    savedFlashlightPerspective, true);
            }
            flashlightAttachmentOverridden = false;
            playing = false;
        }

        private void OnDisable()
        {
            if (!playing) return;
            sequence?.Kill();
            FinishCutscene();
        }

        private void OnDrawGizmosSelected()
        {
            if (cameraRoute == null || cutsceneCameraPoint == null) return;
            Vector3? previous = null;
            foreach (Transform point in cameraRoute)
            {
                if (point == null) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(point.position, cameraCollisionRadius);
                if (previous.HasValue) DrawCameraSegment(previous.Value, point.position);
                previous = point.position;
            }
            if (previous.HasValue) DrawCameraSegment(previous.Value, cutsceneCameraPoint.position);
        }

        private void DrawCameraSegment(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            bool blocked = offset.sqrMagnitude > 0.001f && Physics.SphereCast(
                from, cameraCollisionRadius, offset.normalized, out _, offset.magnitude,
                cameraCollisionMask, QueryTriggerInteraction.Ignore);
            Gizmos.color = blocked ? Color.red : Color.cyan;
            Gizmos.DrawLine(from, to);
        }
    }
}
