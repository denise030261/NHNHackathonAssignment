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

        private float yaw;
        private float pitch;
        private float currentDistance;
        private float distanceVelocity;
        private CameraPerspective previousPerspective;

        public CameraPerspective Perspective => perspective;

        private void Awake()
        {
            previousPerspective = perspective;
            yaw = transform.eulerAngles.y;
            currentDistance = thirdPersonDistance;
        }

        private void Update()
        {
            if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (previousPerspective != perspective)
            {
                SynchronizePerspective();
            }

            yaw += UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch = Mathf.Clamp(
                pitch - UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity,
                minimumPitch,
                maximumPitch);

            if (perspective == CameraPerspective.FirstPerson)
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

            if (perspective == CameraPerspective.FirstPerson)
            {
                UpdateFirstPersonCamera();
            }
            else
            {
                UpdateThirdPersonCamera();
            }
        }

        private void UpdateFirstPersonCamera()
        {
            playerCamera.transform.SetPositionAndRotation(
                transform.TransformPoint(firstPersonOffset),
                Quaternion.Euler(pitch, yaw, 0f));
        }

        private void UpdateThirdPersonCamera()
        {
            Vector3 pivot = transform.position + thirdPersonPivotOffset;
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = orbitRotation * Vector3.back;
            float targetDistance = thirdPersonDistance;

            if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit,
                    thirdPersonDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Max(minimumCameraDistance, hit.distance - collisionPadding);
            }

            currentDistance = Mathf.SmoothDamp(
                currentDistance, targetDistance, ref distanceVelocity, distanceSmoothTime);
            Vector3 cameraPosition = pivot + direction * currentDistance;
            playerCamera.transform.SetPositionAndRotation(cameraPosition, orbitRotation);
        }

        private void SynchronizePerspective()
        {
            yaw = perspective == CameraPerspective.FirstPerson
                ? transform.eulerAngles.y
                : playerCamera.transform.eulerAngles.y;
            pitch = NormalizeAngle(playerCamera.transform.eulerAngles.x);
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            currentDistance = thirdPersonDistance;
            distanceVelocity = 0f;
            previousPerspective = perspective;
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
