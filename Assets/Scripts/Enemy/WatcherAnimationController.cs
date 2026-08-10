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

        [Header("Locomotion")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.1f;

        private int speedParameterHash;
        private Vector3 previousPosition;
        private bool hasPreviousPosition;

        private void OnEnable()
        {
            previousPosition = transform.position;
            hasPreviousPosition = true;
        }

        private void Awake()
        {
            ResolveReferences();
            CacheParameterHash();
        }

        private void Update()
        {
            if (agent == null || animator == null)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            float transformSpeed = hasPreviousPosition && Time.deltaTime > 0f
                ? Vector3.ProjectOnPlane(
                    currentPosition - previousPosition,
                    Vector3.up).magnitude / Time.deltaTime
                : 0f;
            previousPosition = currentPosition;
            hasPreviousPosition = true;

            float planarSpeed = agent.enabled && agent.isOnNavMesh
                ? Vector3.ProjectOnPlane(agent.velocity, Vector3.up).magnitude
                : transformSpeed;
            float animationSpeed = planarSpeed > movementThreshold ? planarSpeed : 0f;

            animator.SetFloat(
                speedParameterHash,
                animationSpeed,
                speedDampTime,
                Time.deltaTime);
        }

        private void OnDisable()
        {
            hasPreviousPosition = false;
            if (animator != null)
            {
                animator.SetFloat(speedParameterHash, 0f);
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
            CacheParameterHash();
        }

        private void ResolveReferences()
        {
            agent ??= GetComponent<NavMeshAgent>();
            animator ??= GetComponentInChildren<Animator>(true);
        }

        private void CacheParameterHash()
        {
            speedParameterHash = Animator.StringToHash(speedParameter);
        }
    }
}
