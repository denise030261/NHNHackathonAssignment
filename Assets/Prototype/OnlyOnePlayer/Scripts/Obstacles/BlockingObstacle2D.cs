using UnityEngine;

namespace OnlyOnePlayer.Prototype.Obstacles
{
    public sealed class BlockingObstacle2D : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private Collider2D obstacleCollider;

        private void Reset()
        {
            obstacleCollider = GetComponent<Collider2D>();
        }

        private void Awake()
        {
            SetupCollider();
        }

        private void OnValidate()
        {
            if (obstacleCollider != null)
            {
                obstacleCollider.isTrigger = false;
            }
        }

        private void SetupCollider()
        {
            if (obstacleCollider == null)
            {
                obstacleCollider = GetComponent<Collider2D>();
            }

            if (obstacleCollider == null)
            {
                obstacleCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            if (obstacleCollider != null)
            {
                obstacleCollider.isTrigger = false;
            }
        }
    }
}
