using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KeyCollectible : MonoBehaviour
    {
        [SerializeField] private string keyId = "Key_01";

        private bool isCollected;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected)
            {
                return;
            }

            PlayerKeyInventory inventory = other.GetComponentInParent<PlayerKeyInventory>();
            if (inventory == null || !inventory.TryCollect(keyId))
            {
                return;
            }

            isCollected = true;
            gameObject.SetActive(false);
        }
    }
}
