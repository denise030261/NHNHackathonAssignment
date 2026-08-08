using UnityEngine;

namespace NHNHackathon.Characters
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraController : MonoBehaviour
    {
        [Header("Perspective")]
        [SerializeField, Tooltip("Active camera style. This value can also be changed during Play Mode.")]
        private CameraPerspective perspective = CameraPerspective.FirstPerson;

        [Header("References")]
        [SerializeField, Tooltip("Camera controlled by this component.")]
        private Camera playerCamera;

        [Header("Mouse Look")]
        [SerializeField, Min(0f), Tooltip("Mouse look sensitivity for both perspectives.")]
        private float mouseSensitivity = 2f;

        [SerializeField, Range(-89f, 0f), Tooltip("Lowest vertical camera angle.")]
        private float minimumPitch = -70f;

        [SerializeField, Range(0f, 89f), Tooltip("Highest vertical camera angle.")]
        private float maximumPitch = 75f;

        [Header("First Person")]
        [SerializeField, Tooltip("Camera position relative to the player origin.")]
        private Vector3 firstPersonOffset = new Vector3(0f, 1.65f, 0.08f);

        [Header("Third Person")]
        [SerializeField, Tooltip("Point around which the third-person camera orbits.")]
        private Vector3 thirdPersonPivotOffset = new Vector3(0f, 1.4f, 0f);

        [SerializeField, Min(0.1f), Tooltip("Desired camera distance from the orbit pivot.")]
        private float thirdPersonDistance = 4f;

        [SerializeField, Min(0.01f), Tooltip("Radius used to prevent the camera passing through walls.")]
        private float collisionRadius = 0.2f;

        [SerializeField, Min(0f), Tooltip("Space retained between the camera and a detected wall.")]
        private float collisionPadding = 0.1f;

        [SerializeField, Min(0.01f), Tooltip("Closest allowed camera distance from the player.")]
        private float minimumCameraDistance = 0.35f;

        [SerializeField, Min(0f), Tooltip("How quickly the camera returns after an obstruction disappears.")]
        private float distanceSmoothTime = 0.08f;

        [SerializeField, Tooltip("Layers considered solid by the third-person camera.")]
        private LayerMask collisionMask = ~(1 << 2);

        [Header("Perspective Transition")]
        [SerializeField, Min(0f), Tooltip("Seconds used to blend between first and third person.")]
        private float perspectiveTransitionDuration = 0.6f;

        [SerializeField, Tooltip("Controls the camera blend progression from 0 to 1.")]
        private AnimationCurve perspectiveTransitionCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float yaw;
        private float pitch;
        private float currentDistance;
        private float distanceVelocity;
        private CameraPerspective previousPerspective;
        private CameraPerspective targetPerspective;
        private bool isTransitioning;
        private float transitionElapsed;
        private float activeTransitionDuration;
        private Vector3 transitionStartPosition;
        private Quaternion transitionStartRotation;
        private bool isForcedTurning;
        private float forcedTurnElapsed;
        private float forcedTurnDuration;
        private float forcedTurnStartYaw;
        private float forcedTurnTargetYaw;

        public CameraPerspective Perspective => targetPerspective;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            previousPerspective = perspective;
            targetPerspective = perspective;
            yaw = transform.eulerAngles.y;
            currentDistance = thirdPersonDistance;
        }

        private void Update()
        {
            if (playerCamera == null)
            {
                return;
            }

            if (isForcedTurning)
            {
                UpdateForcedTurn();
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (previousPerspective != perspective)
            {
                RequestPerspective(perspective);
            }

            if (!isForcedTurning)
            {
                yaw += UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch = Mathf.Clamp(
                    pitch - UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity,
                    minimumPitch,
                    maximumPitch);
            }

            if (targetPerspective == CameraPerspective.FirstPerson)
            {
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private void LateUpdate()
        {
            if (playerCamera == null)
            {
                return;
            }

            if (isTransitioning)
            {
                UpdatePerspectiveTransition();
            }
            else if (targetPerspective == CameraPerspective.FirstPerson)
            {
                ApplyFirstPersonCamera();
            }
            else
            {
                ApplyThirdPersonCamera();
            }
        }

        public void RequestPerspective(CameraPerspective requestedPerspective)
        {
            RequestPerspective(requestedPerspective, perspectiveTransitionDuration);
        }

        public void RequestPerspective(
            CameraPerspective requestedPerspective, float transitionDuration)
        {
            if (playerCamera == null)
            {
                return;
            }

            if (!isTransitioning && targetPerspective == requestedPerspective)
            {
                perspective = requestedPerspective;
                previousPerspective = requestedPerspective;
                return;
            }

            transitionStartPosition = playerCamera.transform.position;
            transitionStartRotation = playerCamera.transform.rotation;
            targetPerspective = requestedPerspective;
            perspective = requestedPerspective;
            previousPerspective = requestedPerspective;
            transitionElapsed = 0f;
            activeTransitionDuration = Mathf.Max(0f, transitionDuration);
            isTransitioning = activeTransitionDuration > 0f;

            SynchronizeLookAngles();
            if (!isTransitioning)
            {
                ApplyTargetPerspectiveCamera();
            }
        }

        public void RequestFacingDirection(Vector3 worldDirection, float turnDuration)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            forcedTurnStartYaw = yaw;
            forcedTurnTargetYaw = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
            forcedTurnElapsed = 0f;
            forcedTurnDuration = Mathf.Max(0f, turnDuration);
            isForcedTurning = forcedTurnDuration > 0f;
            if (!isForcedTurning)
            {
                yaw = forcedTurnTargetYaw;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private void UpdateForcedTurn()
        {
            forcedTurnElapsed += Time.deltaTime;
            float t = forcedTurnDuration <= 0f
                ? 1f
                : Mathf.Clamp01(forcedTurnElapsed / forcedTurnDuration);
            float smoothT = t * t * (3f - 2f * t);
            yaw = Mathf.LerpAngle(forcedTurnStartYaw, forcedTurnTargetYaw, smoothT);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (t >= 1f)
            {
                isForcedTurning = false;
            }
        }

        private void ApplyFirstPersonCamera()
        {
            playerCamera.transform.SetPositionAndRotation(
                transform.TransformPoint(firstPersonOffset),
                Quaternion.Euler(pitch, yaw, 0f));
        }

        private void ApplyThirdPersonCamera()
        {
            CalculateThirdPersonPose(out Vector3 position, out Quaternion rotation);
            playerCamera.transform.SetPositionAndRotation(position, rotation);
        }

        private void UpdatePerspectiveTransition()
        {
            transitionElapsed += Time.deltaTime;
            float normalizedTime = activeTransitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(transitionElapsed / activeTransitionDuration);
            float blend = perspectiveTransitionCurve != null
                ? perspectiveTransitionCurve.Evaluate(normalizedTime)
                : normalizedTime;

            CalculateTargetPose(out Vector3 targetPosition, out Quaternion targetRotation);
            playerCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(transitionStartPosition, targetPosition, blend),
                Quaternion.Slerp(transitionStartRotation, targetRotation, blend));

            if (normalizedTime < 1f)
            {
                return;
            }

            isTransitioning = false;
            ApplyTargetPerspectiveCamera();
        }

        private void CalculateTargetPose(out Vector3 position, out Quaternion rotation)
        {
            if (targetPerspective == CameraPerspective.FirstPerson)
            {
                position = transform.TransformPoint(firstPersonOffset);
                rotation = Quaternion.Euler(pitch, yaw, 0f);
                return;
            }

            CalculateThirdPersonPose(out position, out rotation);
        }

        private void CalculateThirdPersonPose(out Vector3 position, out Quaternion rotation)
        {
            Vector3 pivot = transform.position + thirdPersonPivotOffset;
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            float targetDistance = thirdPersonDistance;

            if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit,
                    thirdPersonDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Max(minimumCameraDistance, hit.distance - collisionPadding);
            }

            currentDistance = Mathf.SmoothDamp(
                currentDistance, targetDistance, ref distanceVelocity, distanceSmoothTime);
            position = pivot + direction * currentDistance;
        }

        private void ApplyTargetPerspectiveCamera()
        {
            if (targetPerspective == CameraPerspective.FirstPerson)
            {
                ApplyFirstPersonCamera();
            }
            else
            {
                ApplyThirdPersonCamera();
            }
        }

        private void SynchronizeLookAngles()
        {
            yaw = playerCamera.transform.eulerAngles.y;
            pitch = NormalizeAngle(playerCamera.transform.eulerAngles.x);
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            distanceVelocity = 0f;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void OnValidate()
        {
            minimumPitch = Mathf.Min(minimumPitch, maximumPitch);
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
    }
}
