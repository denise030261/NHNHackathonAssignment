using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.NPC;
using OnlyOnePlayer.Prototype.Stealth;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Obstacles
{
    public sealed class StunGimmick2D : MonoBehaviour
    {
        [Header("Stun")]
        [SerializeField, Min(0f)] private float stunDuration = 1.5f;

        [Header("Targets")]
        [SerializeField] private bool stunRealPlayer = true;
        [SerializeField] private bool stunNpc = true;
        [SerializeField] private bool stunWatcher = true;

        [Header("Collision")]
        [SerializeField] private Collider2D gimmickCollider;
        [SerializeField] private bool useTriggerCollider = true;
        [SerializeField] private bool stunOnCollision = true;
        [SerializeField] private bool stunOnTrigger = true;

        private void Reset()
        {
            gimmickCollider = GetComponent<Collider2D>();
        }

        private void Awake()
        {
            SetupCollider();
        }

        private void OnValidate()
        {
            if (gimmickCollider != null)
            {
                gimmickCollider.isTrigger = useTriggerCollider;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (stunOnCollision)
            {
                TryStun(collision.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (stunOnTrigger)
            {
                TryStun(other.gameObject);
            }
        }

        private void TryStun(GameObject target)
        {
            if (target == null || !CanStun(target))
            {
                return;
            }

            CharacterStatus status = target.GetComponent<CharacterStatus>();
            if (status != null)
            {
                status.Stun(stunDuration);
            }
        }

        private bool CanStun(GameObject target)
        {
            if (stunRealPlayer && target.GetComponent<RealPlayerIdentity>() != null)
            {
                return true;
            }

            if (stunNpc && target.GetComponent<NpcInputFollower>() != null)
            {
                return true;
            }

            return stunWatcher && target.GetComponent<WatcherController2D>() != null;
        }

        private void SetupCollider()
        {
            if (gimmickCollider == null)
            {
                gimmickCollider = GetComponent<Collider2D>();
            }

            if (gimmickCollider == null)
            {
                gimmickCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            gimmickCollider.isTrigger = useTriggerCollider;
        }
    }
}
