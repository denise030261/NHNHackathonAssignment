using NHNHackathon.LightSystem;
using UnityEngine;

namespace NHNHackathon.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerLocomotionAnimationPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerFlashlightController flashlightController;

        [Header("Animator Parameters")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string flashlightParameter = "FlashlightOn";
        [SerializeField, Min(0f)] private float speedDampTime = 0.08f;

        [Header("Flashlight State Transition")]
        [SerializeField] private string locomotionStateName = "Locomotion";
        [SerializeField] private string flashlightLocomotionStateName = "FlashlightLocomotion";
        [SerializeField, Min(0f)] private float flashlightTransitionDuration = 0.2f;

        private int speedHash;
        private int flashlightHash;
        private int locomotionStateHash;
        private int flashlightLocomotionStateHash;
        private bool previousFlashlightState;
        private bool flashlightStateInitialized;

        private void Awake()
        {
            ResolveReferences();
            CacheParameterHashes();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheParameterHashes();
            ApplyState(true);
        }

        private void Update()
        {
            ApplyState(false);
        }

        private void ApplyState(bool immediate)
        {
            if (animator == null || characterController == null)
            {
                return;
            }

            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            float horizontalSpeed = velocity.magnitude;
            if (immediate || speedDampTime <= 0f)
            {
                animator.SetFloat(speedHash, horizontalSpeed);
            }
            else
            {
                animator.SetFloat(
                    speedHash, horizontalSpeed, speedDampTime, Time.deltaTime);
            }

            bool flashlightEnabled = flashlightController != null
                && flashlightController.IsFlashlightEnabled;
            animator.SetBool(flashlightHash, flashlightEnabled);

            if (!flashlightStateInitialized || immediate)
            {
                previousFlashlightState = flashlightEnabled;
                flashlightStateInitialized = true;
                return;
            }

            if (previousFlashlightState == flashlightEnabled)
            {
                return;
            }

            previousFlashlightState = flashlightEnabled;
            int destinationHash = flashlightEnabled
                ? flashlightLocomotionStateHash
                : locomotionStateHash;
            if (animator.HasState(0, destinationHash))
            {
                animator.speed = 1f;
                animator.CrossFadeInFixedTime(
                    destinationHash, flashlightTransitionDuration, 0, 0f);
            }
        }

        private void ResolveReferences()
        {
            characterController ??= GetComponent<CharacterController>();
            animator ??= GetComponentInChildren<Animator>(true);
            flashlightController ??=
                GetComponentInChildren<PlayerFlashlightController>(true);
        }

        private void CacheParameterHashes()
        {
            speedHash = Animator.StringToHash(speedParameter);
            flashlightHash = Animator.StringToHash(flashlightParameter);
            locomotionStateHash = Animator.StringToHash(locomotionStateName);
            flashlightLocomotionStateHash =
                Animator.StringToHash(flashlightLocomotionStateName);
        }

        private void OnValidate()
        {
            speedDampTime = Mathf.Max(0f, speedDampTime);
            flashlightTransitionDuration = Mathf.Max(0f, flashlightTransitionDuration);
            ResolveReferences();
        }
    }
}
