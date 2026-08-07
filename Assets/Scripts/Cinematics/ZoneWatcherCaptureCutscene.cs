using System.Collections.Generic;
using DG.Tweening;
using NHNHackathon.Enemy;
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
        [SerializeField] private Behaviour[] playerControls;

        [Header("Actors")]
        [SerializeField] private GameObject failedDancer;
        [SerializeField] private Animator failedDancerAnimator;
        [SerializeField] private string wrongDanceStateName = "Dance4";
        [SerializeField] private EnemyController watcherController;
        [SerializeField] private NavMeshAgent watcherAgent;
        [SerializeField] private Animator watcherAnimator;
        [SerializeField] private WatcherCapturePresenter capturePresenter;

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
        [SerializeField, Min(0.01f)] private float corridorSegmentDuration = 1f;
        [SerializeField, Min(0.01f)] private float cameraReturnDuration = 0.9f;

        private readonly List<(Behaviour control, bool enabled)> controlStates = new();
        private Sequence sequence;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private Vector3 watcherStartPosition;
        private Quaternion watcherStartRotation;
        private Transform dancerOriginalParent;
        private bool watcherWasEnabled;
        private bool agentWasEnabled;
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
            controlStates.Clear();
            AddAndDisable(playerCameraController);
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
            sequence.AppendCallback(PlayCaptureMotion);
            sequence.AppendInterval(captureHoldDuration + 1.2f);
            sequence.AppendCallback(AttachDancerToWatcher);
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

        private void PlayCaptureMotion()
        {
            if (capturePresenter != null)
                capturePresenter.CreateCaptureTween(failedDancer.transform);
        }

        private void AttachDancerToWatcher()
        {
            failedDancer.transform.SetParent(watcherController.transform, true);
        }

        private void AppendCorridorMovement(Sequence target)
        {
            if (corridorRoute == null) return;
            foreach (Transform point in corridorRoute)
            {
                if (point == null) continue;
                target.Append(watcherController.transform.DOMove(point.position, corridorSegmentDuration).SetEase(Ease.InOutSine));
                target.Join(watcherController.transform.DORotateQuaternion(
                    LookRotation(watcherController.transform.position, point), Mathf.Min(0.35f, corridorSegmentDuration)));
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
            failedDancer.transform.SetParent(dancerOriginalParent, true);
            failedDancer.SetActive(false);
            if (capturePresenter != null)
            {
                capturePresenter.enabled = false;
                capturePresenter.enabled = true;
            }
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
            if (watcherAnimator != null) watcherAnimator.speed = 1f;
            foreach ((Behaviour control, bool wasEnabled) in controlStates)
                if (control != null) control.enabled = wasEnabled;
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
