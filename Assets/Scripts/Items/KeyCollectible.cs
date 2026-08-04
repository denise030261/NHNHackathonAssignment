using NHNHackathon.Interaction;
using NHNHackathon.Inspection;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KeyCollectible : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Single source of truth for the key ID, name, icon, and inspection data.")]
        private ItemDefinition itemDefinition;

        private bool isCollected;

        public string InteractionPrompt => "\uC5F4\uC1E0 \uD68D\uB4DD";

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return !isCollected
                && itemDefinition != null
                && itemDefinition.Type == ItemType.Key
                && interactor != null
                && interactor.GetComponent<PlayerItemInventory>() != null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            PlayerItemInventory itemInventory =
                interactor.GetComponent<PlayerItemInventory>();
            if (!itemInventory.TryCollect(itemDefinition))
            {
                return;
            }

            interactor.KeyInventory?.TryCollect(itemDefinition.ItemId);

            isCollected = true;
            gameObject.SetActive(false);
            GameProgressionController.Instance?.TryComplete(
                itemDefinition != null ? itemDefinition.ProgressionCondition : null);

            if (itemDefinition != null && itemDefinition.InspectOnPickup)
            {
                ItemInspectionController.Instance?.Open(itemDefinition);
            }
        }

        private void OnValidate()
        {
            if (itemDefinition != null && itemDefinition.Type != ItemType.Key)
            {
                Debug.LogWarning(
                    $"{name}: KeyCollectible requires an ItemDefinition whose type is Key.", this);
            }
        }
    }
}
