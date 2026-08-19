using System.Collections.Generic;
using NHNHackathon.Characters;
using NHNHackathon.Items;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NHNHackathon.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private const int NearbyColliderCapacity = 64;
        private const float DefaultNearbyOutlinePixels = 1.5f;
        private const float DefaultFocusedOutlinePixels = 4f;

        [Header("Input")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;
        [SerializeField, Min(0f)] private float detectionRadius = 0.3f;
        [SerializeField, Min(0f)] private float thirdPersonOriginHeight = 1.2f;
        [SerializeField] private LayerMask interactionMask = ~(1 << 2);

        [Header("References")]
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private Camera playerCamera;

        [Header("Interaction UI")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private Color promptColor = Color.white;
        [SerializeField] private Color messageColor = new Color(1f, 0.72f, 0.2f);

        [Header("Interaction Outline")]
        [SerializeField] private bool showInteractionOutline = true;
        [SerializeField, Tooltip("When enabled, doors are excluded and only collectible items are outlined.")]
        private bool outlineOnlyCollectibleItems = true;
        [SerializeField] private Color outlineColor =
            new Color(1f, 0.78f, 0.2f, 1f);
        [SerializeField, Min(0.1f)] private float nearbyOutlineRadius = 3f;
        [SerializeField, Min(0f)] private float nearbyOutlineOriginHeight = 0.75f;
        [FormerlySerializedAs("nearbyOutlineWidth")]
        [SerializeField, Range(0.5f, 12f)]
        private float nearbyOutlinePixels = 1.5f;
        [FormerlySerializedAs("outlineWidth")]
        [FormerlySerializedAs("focusedOutlineWidth")]
        [SerializeField, Range(0.5f, 12f)]
        private float focusedOutlinePixels = 4f;

        private IInteractable currentInteractable;
        private readonly Collider[] nearbyColliders =
            new Collider[NearbyColliderCapacity];
        private readonly HashSet<InteractionOutline> frameOutlines = new();
        private readonly List<InteractionOutline> visibleOutlines = new();
        private string temporaryMessage;
        private float messageExpiresAt;

        //public PlayerKeyInventory KeyInventory => keyInventory;

        private void Awake()
        {
            MigrateLegacyOutlineWidths();
        }

        private void Update()
        {
            SetCurrentInteractable(FindInteractable());
            RefreshInteractionOutlines();
            RefreshPrompt();
            if (currentInteractable != null
                && currentInteractable.CanInteract(this)
                && UnityEngine.Input.GetKeyDown(interactionKey))
            {
                currentInteractable.Interact(this);
            }
        }

        private void SetCurrentInteractable(IInteractable interactable)
        {
            currentInteractable = interactable;
        }

        private void RefreshInteractionOutlines()
        {
            frameOutlines.Clear();
            if (!showInteractionOutline)
            {
                ClearVisibleOutlines();
                return;
            }

            Vector3 origin = transform.position
                + Vector3.up * nearbyOutlineOriginHeight;
            int count = Physics.OverlapSphereNonAlloc(
                origin, nearbyOutlineRadius, nearbyColliders,
                interactionMask, QueryTriggerInteraction.Collide);
            for (int index = 0; index < count; index++)
            {
                IInteractable nearby = FindOutlineTarget(nearbyColliders[index]);
                ApplyOutline(nearby, nearbyOutlinePixels);
                nearbyColliders[index] = null;
            }

            ApplyRegisteredItemOutlines(origin);

            ApplyOutline(currentInteractable, focusedOutlinePixels);

            for (int index = visibleOutlines.Count - 1; index >= 0; index--)
            {
                InteractionOutline outline = visibleOutlines[index];
                if (outline == null || !frameOutlines.Contains(outline))
                {
                    if (outline != null)
                    {
                        outline.SetHighlighted(false);
                    }
                    visibleOutlines.RemoveAt(index);
                }
            }

            foreach (InteractionOutline outline in frameOutlines)
            {
                if (!visibleOutlines.Contains(outline))
                {
                    visibleOutlines.Add(outline);
                }
            }
        }

        private void ApplyRegisteredItemOutlines(Vector3 origin)
        {
            float radiusSquared = nearbyOutlineRadius * nearbyOutlineRadius;
            foreach (IInteractable interactable in
                     InteractableRegistry.ActiveInteractables)
            {
                if (!ShouldOutline(interactable)
                    || interactable is not Component target
                    || !target.gameObject.activeInHierarchy
                    || !interactable.CanInteract(this))
                {
                    continue;
                }

                Collider itemCollider = target.GetComponent<Collider>();
                if (itemCollider == null || !itemCollider.enabled)
                {
                    continue;
                }

                Vector3 closestPoint = itemCollider.ClosestPoint(origin);
                if ((closestPoint - origin).sqrMagnitude <= radiusSquared)
                {
                    ApplyOutline(interactable, nearbyOutlinePixels);
                }
            }
        }

        private IInteractable FindOutlineTarget(Collider candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            foreach (MonoBehaviour behaviour in
                     candidate.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IInteractable interactable
                    && interactable.CanInteract(this)
                    && ShouldOutline(interactable))
                {
                    return interactable;
                }
            }

            return null;
        }

        private void ApplyOutline(IInteractable interactable, float pixels)
        {
            if (!ShouldOutline(interactable)
                || interactable is not Component target)
            {
                return;
            }

            InteractionOutline outline = target.GetComponent<InteractionOutline>()
                ?? target.gameObject.AddComponent<InteractionOutline>();
            outline.Configure(outlineColor, pixels);
            if (frameOutlines.Add(outline))
            {
                outline.SetHighlighted(true);
            }
        }

        private void ClearVisibleOutlines()
        {
            foreach (InteractionOutline outline in visibleOutlines)
            {
                if (outline != null)
                {
                    outline.SetHighlighted(false);
                }
            }
            visibleOutlines.Clear();
            frameOutlines.Clear();
        }

        private bool ShouldOutline(IInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            return !outlineOnlyCollectibleItems
                || interactable is InspectableItem
                || interactable is KeyCollectible;
        }

        public void ShowTemporaryMessage(string message, float duration)
        {
            temporaryMessage = message;
            messageExpiresAt = Time.unscaledTime + duration;
            RefreshPrompt();
        }

        private IInteractable FindInteractable()
        {
            if (cameraController == null || playerCamera == null)
            {
                return null;
            }

            Vector3 origin;
            Vector3 direction;
            if (cameraController.Perspective == CameraPerspective.FirstPerson)
            {
                origin = playerCamera.transform.position;
                direction = playerCamera.transform.forward;
            }
            else
            {
                origin = transform.position + Vector3.up * thirdPersonOriginHeight;
                direction = transform.forward;
            }

            if (!Physics.SphereCast(
                    origin, detectionRadius, direction, out RaycastHit hit,
                    interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return null;
            }

            foreach (MonoBehaviour behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IInteractable interactable && interactable.CanInteract(this))
                {
                    return interactable;
                }
            }

            return null;
        }

        private void RefreshPrompt()
        {
            bool hasTemporaryMessage = Time.unscaledTime < messageExpiresAt;
            string text = hasTemporaryMessage
                ? temporaryMessage
                : currentInteractable != null && currentInteractable.CanInteract(this)
                    ? $"[{interactionKey}] {currentInteractable.InteractionPrompt}"
                    : string.Empty;

            if (promptRoot != null)
            {
                promptRoot.SetActive(!string.IsNullOrEmpty(text));
            }
            if (promptText != null)
            {
                promptText.text = text;
                promptText.color = hasTemporaryMessage ? messageColor : promptColor;
            }
        }

        private void OnDisable()
        {
            SetCurrentInteractable(null);
            ClearVisibleOutlines();
            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }

        private void OnValidate()
        {
            MigrateLegacyOutlineWidths();
            nearbyOutlineRadius = Mathf.Max(0.1f, nearbyOutlineRadius);
            nearbyOutlineOriginHeight = Mathf.Max(0f, nearbyOutlineOriginHeight);
            nearbyOutlinePixels = Mathf.Clamp(
                nearbyOutlinePixels, 0.5f, 12f);
            focusedOutlinePixels = Mathf.Clamp(
                focusedOutlinePixels, nearbyOutlinePixels, 12f);
            cameraController ??= GetComponent<PlayerCameraController>();
            playerCamera ??= GetComponentInChildren<Camera>(true);
        }

        private void MigrateLegacyOutlineWidths()
        {
            if (nearbyOutlinePixels < 0.5f)
            {
                nearbyOutlinePixels = DefaultNearbyOutlinePixels;
            }
            if (focusedOutlinePixels < 0.5f)
            {
                focusedOutlinePixels = DefaultFocusedOutlinePixels;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(
                outlineColor.r, outlineColor.g, outlineColor.b, 0.35f);
            Vector3 origin = transform.position
                + Vector3.up * nearbyOutlineOriginHeight;
            Gizmos.DrawWireSphere(origin, nearbyOutlineRadius);
        }
    }
}
