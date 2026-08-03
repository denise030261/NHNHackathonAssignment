using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WatcherAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;

        [Header("Walk Animation")]
        [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
        [SerializeField, Min(0.01f)] private float playbackSpeed = 1f;

        private void Awake()
        {
            agent ??= GetComponent<NavMeshAgent>();
            animator ??= GetComponentInChildren<Animator>(true);
        }

        private void Update()
        {
            if (agent == null || animator == null)
            {
                return;
            }

            float planarSpeed = Vector3.ProjectOnPlane(agent.velocity, Vector3.up).magnitude;
            animator.speed = planarSpeed > movementThreshold ? playbackSpeed : 0f;
        }

        private void OnDisable()
        {
            if (animator != null)
            {
                animator.speed = 0f;
            }
        }

        private void OnValidate()
        {
            agent ??= GetComponent<NavMeshAgent>();
            animator ??= GetComponentInChildren<Animator>(true);
        }
    }
}
