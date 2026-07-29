using NHNHackathon.Interaction;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KeyCollectible : MonoBehaviour, IInteractable
    {
        [SerializeField] private string keyId = "Key_01";

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

            isCollected = true;
            gameObject.SetActive(false);
        }
    }
}
