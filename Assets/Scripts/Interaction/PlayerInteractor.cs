using NHNHackathon.Characters;
using NHNHackathon.Items;
using UnityEngine;

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
        [SerializeField] private PlayerKeyInventory keyInventory;

        private IInteractable currentInteractable;
        private string temporaryMessage;
        private float messageExpiresAt;

        public PlayerKeyInventory KeyInventory => keyInventory;

        private void Update()
        {
            currentInteractable = FindInteractable();
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
            messageExpiresAt = Time.time + duration;
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
                if (behaviour is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private void OnGUI()
        {
            bool hasTemporaryMessage = Time.time < messageExpiresAt;
            string text = hasTemporaryMessage
                ? temporaryMessage
                : currentInteractable != null && currentInteractable.CanInteract(this)
                    ? $"[{interactionKey}] {currentInteractable.InteractionPrompt}"
                    : string.Empty;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = hasTemporaryMessage ? 30 : 26,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = hasTemporaryMessage
                ? new Color(1f, 0.72f, 0.2f)
                : Color.white;
            GUI.Label(
                new Rect(0f, Screen.height * 0.72f, Screen.width, 55f),
                text,
                style);
        }

        private void OnValidate()
        {
            cameraController ??= GetComponent<PlayerCameraController>();
            playerCamera ??= GetComponentInChildren<Camera>(true);
            keyInventory ??= GetComponent<PlayerKeyInventory>();
        }
    }
}
