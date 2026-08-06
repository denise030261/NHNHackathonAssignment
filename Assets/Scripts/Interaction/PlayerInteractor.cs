using NHNHackathon.Characters;
using NHNHackathon.Items;
using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
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

        private IInteractable currentInteractable;
        private string temporaryMessage;
        private float messageExpiresAt;

        //public PlayerKeyInventory KeyInventory => keyInventory;

        private void Update()
        {
            currentInteractable = FindInteractable();
            RefreshPrompt();
            if (currentInteractable != null
                && currentInteractable.CanInteract(this)
                && UnityEngine.Input.GetKeyDown(interactionKey))
            {
                currentInteractable.Interact(this);
            }
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
            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }

        private void OnValidate()
        {
            cameraController ??= GetComponent<PlayerCameraController>();
            playerCamera ??= GetComponentInChildren<Camera>(true);
        }
    }
}
