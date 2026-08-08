using NHNHackathon.Inspection;
using NHNHackathon.Interaction;
using NHNHackathon.Progression;
using NHNHackathon.AudioSystem;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class InspectableItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private string interactionPrompt = "아이템 획득";

        private bool isCollected;

        public string InteractionPrompt => interactionPrompt;
        public ItemDefinition Item => item;

        public void ApplyCollectedState()
        {
            isCollected = true;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return !isCollected && item != null && interactor != null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            PlayerItemInventory inventory = interactor.GetComponent<PlayerItemInventory>();
            if (inventory == null || !inventory.TryCollect(item))
            {
                return;
            }

            isCollected = true;
            if (item.Type == ItemType.Paper)
            {
                GameSfxPlayer.PlayPaperPickup(transform.position);
            }
            else if (item.ItemId == "Flashlight")
            {
                GameSfxPlayer.PlayFlashlightPickup(transform.position);
            }
            gameObject.SetActive(false);
            GameProgressionController.Instance?.TryComplete(item.ProgressionCondition);

            if (item.InspectOnPickup)
            {
                ItemInspectionController.Instance?.Open(item);
            }
        }
    }
}
