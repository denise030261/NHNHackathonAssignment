using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ExitSuccessZone : MonoBehaviour
    {
        [SerializeField] private ExitDoor exitDoor;
        [SerializeField] private GameSuccessController successController;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (exitDoor == null || !exitDoor.IsOpen)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerKeyInventory>() != null)
            {
                successController?.TriggerSuccess();
            }
        }
    }
}
