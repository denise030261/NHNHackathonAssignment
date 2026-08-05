using NHNHackathon.Characters;
using UnityEngine;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyCaptureTrigger : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;

        private void Awake()
        {
            enemyController ??= GetComponentInParent<EnemyController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                enemyController?.TryCapturePlayer(player.transform);
            }
        }

        private void OnValidate()
        {
            enemyController ??= GetComponentInParent<EnemyController>();
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }
    }
}
