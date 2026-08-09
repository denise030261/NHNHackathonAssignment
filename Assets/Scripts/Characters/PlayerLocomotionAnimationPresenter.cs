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

        private int speedHash;
        private int flashlightHash;

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

            animator.SetBool(
                flashlightHash,
                flashlightController != null && flashlightController.IsFlashlightEnabled);
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
        }

        private void OnValidate()
        {
            speedDampTime = Mathf.Max(0f, speedDampTime);
            ResolveReferences();
        }
    }
}
