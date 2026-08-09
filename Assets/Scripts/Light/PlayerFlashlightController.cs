using System.Collections;
using NHNHackathon.AudioSystem;
using NHNHackathon.Characters;
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.LightSystem
{
    [DisallowMultipleComponent]
    public sealed class PlayerFlashlightController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F;
        [SerializeField] private Light flashlight;
        [SerializeField] private bool startEnabled;

        [Header("Perspective Attachment")]
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private Transform firstPersonParent;
        [SerializeField] private Vector3 firstPersonLocalPosition = new(0f, 0f, 0.15f);
        [SerializeField] private Vector3 firstPersonLocalEulerAngles;
        [SerializeField] private Transform thirdPersonParent;
        [SerializeField] private Vector3 thirdPersonLocalPosition;
        [SerializeField] private Vector3 thirdPersonLocalEulerAngles;
        [SerializeField, Min(0f)] private float attachmentTransitionDuration = 0.45f;

        [Header("Held Flashlight Mesh")]
        [SerializeField, Tooltip("Physical model attached to the right-hand socket. It does not control the light direction.")]
        private GameObject heldFlashlightMesh;
        [SerializeField, Min(0.01f)] private float heldFlashlightMeshScale = 1.25f;

        [Header("Inventory Requirement")]
        [SerializeField] private PlayerItemInventory playerInventory;
        [SerializeField, Tooltip("The flashlight can only be toggled while this item is owned.")]
        private ItemDefinition requiredFlashlightItem;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private string missingItemMessage = "손전등이 필요합니다.";
        [SerializeField, Min(0f)] private float missingItemMessageDuration = 1.5f;

        public bool CanUseFlashlight => requiredFlashlightItem == null
            || playerInventory != null && playerInventory.Contains(requiredFlashlightItem);
        public bool IsFlashlightEnabled => flashlight != null && flashlight.enabled;
        public CameraPerspective AttachmentPerspective => currentPerspective;

        /// <summary>
        /// Moves the flashlight without changing whether its light is on or off.
        /// Cutscenes use an immediate move because this behaviour is disabled while
        /// player input is locked.
        /// </summary>
        public void SetAttachmentPerspective(CameraPerspective perspective, bool immediate = true)
        {
            currentPerspective = perspective;
            ApplyAttachment(perspective, immediate);
        }

        private void Awake()
        {
            playerInventory ??= GetComponentInParent<PlayerItemInventory>();
            playerInteractor ??= GetComponentInParent<PlayerInteractor>();
            cameraController ??= GetComponentInParent<PlayerCameraController>();
            currentPerspective = cameraController != null
                ? cameraController.Perspective
                : CameraPerspective.FirstPerson;
            ApplyHeldMeshScale();
            if (heldFlashlightMesh != null)
            {
                heldFlashlightMesh.SetActive(false);
            }
            ApplyAttachment(currentPerspective, true);
        }

        private void OnEnable()
        {
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void Start()
        {
            SetFlashlight(startEnabled && CanUseFlashlight);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                if (!CanUseFlashlight)
                {
                    SetFlashlight(false);
                    playerInteractor?.ShowTemporaryMessage(
                        missingItemMessage, missingItemMessageDuration);
                    return;
                }
                SetFlashlight(!flashlight.enabled);
                GameSfxPlayer.PlayFlashlightToggle(transform.position);
            }
        }

        private CameraPerspective currentPerspective;
        private Coroutine attachmentRoutine;

        private void LateUpdate()
        {
            if (cameraController == null || cameraController.Perspective == currentPerspective)
            {
                return;
            }

            currentPerspective = cameraController.Perspective;
            ApplyAttachment(currentPerspective, false);
        }

        private void ApplyAttachment(CameraPerspective perspective, bool immediate)
        {
            Transform targetParent = perspective == CameraPerspective.FirstPerson
                ? firstPersonParent
                : thirdPersonParent;
            Vector3 targetLocalPosition = perspective == CameraPerspective.FirstPerson
                ? firstPersonLocalPosition
                : thirdPersonLocalPosition;
            Vector3 targetLocalEulerAngles = perspective == CameraPerspective.FirstPerson
                ? firstPersonLocalEulerAngles
                : thirdPersonLocalEulerAngles;
            if (targetParent == null)
            {
                return;
            }

            if (attachmentRoutine != null)
            {
                StopCoroutine(attachmentRoutine);
                attachmentRoutine = null;
            }

            if (immediate || attachmentTransitionDuration <= 0f)
            {
                transform.SetParent(targetParent, false);
                transform.localPosition = targetLocalPosition;
                transform.localRotation = Quaternion.Euler(targetLocalEulerAngles);
                return;
            }

            attachmentRoutine = StartCoroutine(TransitionAttachment(
                targetParent, targetLocalPosition,
                Quaternion.Euler(targetLocalEulerAngles)));
        }

        private IEnumerator TransitionAttachment(
            Transform targetParent, Vector3 targetLocalPosition,
            Quaternion targetLocalRotation)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            transform.SetParent(null, true);
            float elapsed = 0f;
            while (elapsed < attachmentTransitionDuration && targetParent != null)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / attachmentTransitionDuration));
                Vector3 targetPosition = targetParent.TransformPoint(targetLocalPosition);
                Quaternion targetRotation = targetParent.rotation * targetLocalRotation;
                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, progress),
                    Quaternion.Slerp(startRotation, targetRotation, progress));
                yield return null;
            }

            if (targetParent != null)
            {
                transform.SetParent(targetParent, false);
                transform.localPosition = targetLocalPosition;
                transform.localRotation = targetLocalRotation;
            }
            attachmentRoutine = null;
        }

        private void OnDisable()
        {
            if (attachmentRoutine != null)
            {
                StopCoroutine(attachmentRoutine);
                attachmentRoutine = null;
            }
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandleInventoryChanged()
        {
            if (!CanUseFlashlight)
            {
                SetFlashlight(false);
            }
        }

        private void SetFlashlight(bool value)
        {
            if (flashlight != null)
            {
                flashlight.enabled = value;
            }
            if (heldFlashlightMesh != null)
            {
                heldFlashlightMesh.SetActive(value);
            }
        }

        private void ApplyHeldMeshScale()
        {
            if (heldFlashlightMesh != null)
            {
                heldFlashlightMesh.transform.localScale =
                    Vector3.one * Mathf.Max(0.01f, heldFlashlightMeshScale);
            }
        }

        private void OnValidate()
        {
            if (flashlight == null)
            {
                flashlight = GetComponent<Light>();
            }
            playerInventory ??= GetComponentInParent<PlayerItemInventory>();
            playerInteractor ??= GetComponentInParent<PlayerInteractor>();
            cameraController ??= GetComponentInParent<PlayerCameraController>();
            heldFlashlightMeshScale = Mathf.Max(0.01f, heldFlashlightMeshScale);
            ApplyHeldMeshScale();
            if (requiredFlashlightItem != null
                && requiredFlashlightItem.Type != ItemType.General)
            {
                Debug.LogWarning(
                    $"{name}: Required Flashlight Item should use the General item type.", this);
            }
        }
    }
}
