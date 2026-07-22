using UnityEngine;

namespace NHNHackathon.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f), Tooltip("Player movement speed in metres per second.")]
        private float moveSpeed = 4f;

        [SerializeField, Min(0f), Tooltip("How quickly the player faces the movement direction in third person.")]
        private float rotationSpeed = 12f;

        [SerializeField, Tooltip("Downward acceleration applied while airborne.")]
        private float gravity = -20f;

        [Header("References")]
        [SerializeField, Tooltip("Camera whose horizontal axes determine the movement direction.")]
        private Transform movementCamera;

        [SerializeField, Tooltip("Camera controller used to determine the active perspective.")]
        private PlayerCameraController cameraController;

        private CharacterController characterController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (movementCamera == null || cameraController == null)
            {
                return;
            }

            Vector2 input = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 cameraForward = Vector3.ProjectOnPlane(movementCamera.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(movementCamera.right, Vector3.up).normalized;
            Vector3 horizontalVelocity = (cameraForward * input.y + cameraRight * input.x) * moveSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            if (cameraController.Perspective == CameraPerspective.ThirdPerson && horizontalVelocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void OnValidate()
        {
            if (movementCamera == null)
            {
                Camera mainCamera = Camera.main;
                movementCamera = mainCamera != null ? mainCamera.transform : null;
            }

            if (cameraController == null)
            {
                cameraController = GetComponent<PlayerCameraController>();
            }
        }
    }
}
