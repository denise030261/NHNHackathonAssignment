using NHNHackathon.Interaction;
using NHNHackathon.Inspection;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KeyCollectible : MonoBehaviour, IInteractable
    {
        [SerializeField] private string keyId = "Key_01";
        [SerializeField, Tooltip("Shared item data used by the inspection screen.")]
        private ItemDefinition itemDefinition;

        private bool isCollected;

        public string InteractionPrompt => "\uC5F4\uC1E0 \uD68D\uB4DD";

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return !isCollected && interactor != null && interactor.KeyInventory != null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)
                || !interactor.KeyInventory.TryCollect(keyId))
            {
                return;
            }

            PlayerItemInventory itemInventory =
                interactor.GetComponent<PlayerItemInventory>();
            if (itemDefinition != null && itemInventory != null)
            {
                itemInventory.TryCollect(itemDefinition);
            }

            isCollected = true;
            gameObject.SetActive(false);

            if (itemDefinition != null && itemDefinition.InspectOnPickup)
            {
                ItemInspectionController.Instance?.Open(itemDefinition);
            }
        }
    }
}
