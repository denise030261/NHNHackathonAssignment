using NHNHackathon.Dance;
using NHNHackathon.Game;
using NHNHackathon.LightSystem;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerDisguiseState playerDisguise;
        [SerializeField] private EnemyPatrolRoute patrolRoute;
        [SerializeField] private GameOverController gameOverController;
        [SerializeField] private Renderer enemyRenderer;

        [Header("Patrol")]
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0f)] private float pointArrivalDistance = 0.35f;
        [SerializeField, Min(0f)] private float pointWaitTime = 0.5f;

        [Header("Light Investigation")]
        [SerializeField, Min(0.02f)] private float lightDestinationUpdateInterval = 0.15f;
        [SerializeField, Min(0f)] private float lightLostGraceTime = 0.5f;
        [SerializeField, Min(0f)] private float maximumInvestigationTime = 10f;
        [SerializeField, Min(0.1f), Tooltip("Radius used to map the illuminated point onto reachable NavMesh.")]
        private float lightNavMeshSampleRadius = 3f;

        [Header("Chase")]
        [SerializeField, Min(0f)] private float chaseSpeed = 4.5f;
        [SerializeField, Min(0f)] private float loseDistance = 14f;
        [SerializeField, Min(0f)] private float minimumDisguiseDistance = 3f;
        [SerializeField, Min(0f), Tooltip("Seconds the enemy follows the last seen position after losing sight.")]
        private float lostSightGraceDuration = 2f;

        [Header("Suspicion")]
        [SerializeField, Min(0f)] private float suspicionDuration = 3f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackDistance = 1.5f;
        [SerializeField, Min(0f)] private float maximumAttackHeightDifference = 1f;

        [Header("Performance")]
        [SerializeField, Min(0.02f)] private float perceptionInterval = 0.1f;

        private NavMeshAgent agent;
        private EnemyPerception perception;
        private int patrolIndex;
        private float waitUntil;
        private float nextPerceptionTime;
        private float suspicionEndsAt;
        private float investigationStartedAt;
        private float nextLightDestinationUpdate;
        private float lightLostAt;
        private bool hasPatrolDestination;
        private LightStimulusSource investigatedLight;
        private Vector3 lastKnownLightPosition;
        private Vector3 lastKnownPlayerPosition;
        private MaterialPropertyBlock propertyBlock;
        private bool hasLostSight;
        private float lostSightStartedAt;

        public EnemyState CurrentState { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<EnemyPerception>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ChangeState(EnemyState.Roaming);
        }

        private void Update()
        {
            if (gameOverController != null && gameOverController.IsGameOver)
            {
                return;
            }

            if (Time.time >= nextPerceptionTime)
            {
                nextPerceptionTime = Time.time + perceptionInterval;
                EvaluatePlayer();
            }

            switch (CurrentState)
            {
                case EnemyState.Roaming:
                    UpdateRoaming();
                    break;
                case EnemyState.InvestigatingLight:
                    UpdateLightInvestigation();
                    break;
                case EnemyState.Chasing:
                    UpdateChasing();
                    break;
                case EnemyState.Suspicious:
                    UpdateSuspicious();
                    break;
            }
        }

        private void EvaluatePlayer()
        {
            if (player == null || CurrentState == EnemyState.Attacking)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            bool isDisguised = playerDisguise != null && playerDisguise.IsDisguised;

            switch (CurrentState)
            {
                case EnemyState.Roaming:
                case EnemyState.InvestigatingLight:
                    EvaluatePlayerWhileRoaming(isDisguised);
                    break;
                case EnemyState.Chasing:
                    EvaluatePlayerWhileChasing(isDisguised, distance);
                    break;
                case EnemyState.Suspicious:
                    EvaluatePlayerWhileSuspicious(isDisguised, distance);
                    break;
            }
        }

        private void EvaluatePlayerWhileRoaming(bool isDisguised)
        {
            // A valid disguise is always trusted until this enemy has already begun a chase.
            if (isDisguised)
            {
                return;
            }

            if (TryAttackPlayer())
            {
                return;
            }

            if (perception.CanSeeTarget(player))
            {
                ChangeState(EnemyState.Chasing);
            }
        }

        private void EvaluatePlayerWhileChasing(bool isDisguised, float distance)
        {
            if (TryAttackPlayer())
            {
                return;
            }

            if (isDisguised && distance >= minimumDisguiseDistance)
            {
                ChangeState(EnemyState.Suspicious);
                return;
            }

            if (distance >= loseDistance)
            {
                ChangeState(EnemyState.Suspicious);
                return;
            }

            if (perception.CanSeeTarget(player))
            {
                lastKnownPlayerPosition = player.position;
                hasLostSight = false;
                return;
            }

            if (!hasLostSight)
            {
                hasLostSight = true;
                lostSightStartedAt = Time.time;
            }

            if (Time.time - lostSightStartedAt >= lostSightGraceDuration)
            {
                ChangeState(EnemyState.Suspicious);
            }
        }

        private void EvaluatePlayerWhileSuspicious(bool isDisguised, float distance)
        {
            if (TryAttackPlayer())
            {
                return;
            }

            if (isDisguised && distance >= minimumDisguiseDistance)
            {
                return;
            }

            if (perception.CanSeeTarget(player))
            {
                ChangeState(EnemyState.Chasing);
            }
        }

        private bool TryAttackPlayer()
        {
            if (!perception.HasClearAttackPath(player, attackDistance, maximumAttackHeightDifference))
            {
                return false;
            }

            Attack();
            return true;
        }

        private void UpdateRoaming()
        {
            if (TryFindVisibleLight(out LightStimulusSource light))
            {
                investigatedLight = light;
                TryResolveLightDestination(light, out lastKnownLightPosition);
                ChangeState(EnemyState.InvestigatingLight);
                return;
            }

            Transform point = patrolRoute != null ? patrolRoute.GetPoint(patrolIndex) : null;
            if (point == null || !agent.isOnNavMesh)
            {
                return;
            }

            if (!hasPatrolDestination)
            {
                if (Time.time >= waitUntil)
                {
                    hasPatrolDestination = agent.SetDestination(point.position);
                }
                return;
            }

            if (HasReachedDestination())
            {
                patrolIndex = (patrolIndex + 1) % patrolRoute.Count;
                waitUntil = Time.time + pointWaitTime;
                hasPatrolDestination = false;
                agent.ResetPath();
            }
        }

        private void UpdateLightInvestigation()
        {
            bool canSeeLight = investigatedLight != null && investigatedLight.IsActive
                && perception.CanSeePoint(investigatedLight.Position, true);
            if (canSeeLight)
            {
                TryResolveLightDestination(investigatedLight, out lastKnownLightPosition);
                lightLostAt = float.PositiveInfinity;
                if (Time.time >= nextLightDestinationUpdate && agent.isOnNavMesh)
                {
                    nextLightDestinationUpdate = Time.time + lightDestinationUpdateInterval;
                    agent.SetDestination(lastKnownLightPosition);
                }

                if (HasReachedPosition(lastKnownLightPosition) && !investigatedLight.IsMoving)
                {
                    ChangeState(EnemyState.Roaming);
                }
            }
            else
            {
                if (float.IsPositiveInfinity(lightLostAt))
                {
                    lightLostAt = Time.time;
                }

                if (Time.time - lightLostAt >= lightLostGraceTime && agent.isOnNavMesh)
                {
                    agent.SetDestination(lastKnownLightPosition);
                }

                if (HasReachedPosition(lastKnownLightPosition))
                {
                    ChangeState(EnemyState.Roaming);
                }
            }

            if (maximumInvestigationTime > 0f
                && Time.time - investigationStartedAt >= maximumInvestigationTime)
            {
                ChangeState(EnemyState.Roaming);
            }
        }

        private void UpdateChasing()
        {
            if (player != null && agent.isOnNavMesh)
            {
                agent.SetDestination(hasLostSight ? lastKnownPlayerPosition : player.position);
            }
        }

        private void UpdateSuspicious()
        {
            if (Time.time >= suspicionEndsAt)
            {
                ChangeState(EnemyState.Roaming);
            }
        }

        private void ChangeState(EnemyState newState)
        {
            CurrentState = newState;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = newState is EnemyState.Suspicious or EnemyState.Attacking;
                if (agent.isStopped)
                {
                    agent.ResetPath();
                }
            }

            switch (newState)
            {
                case EnemyState.Roaming:
                    agent.speed = patrolSpeed;
                    investigatedLight = null;
                    hasPatrolDestination = false;
                    waitUntil = Time.time;
                    break;
                case EnemyState.InvestigatingLight:
                    agent.speed = patrolSpeed;
                    hasPatrolDestination = false;
                    investigationStartedAt = Time.time;
                    nextLightDestinationUpdate = 0f;
                    lightLostAt = float.PositiveInfinity;
                    break;
                case EnemyState.Chasing:
                    agent.speed = chaseSpeed;
                    investigatedLight = null;
                    hasPatrolDestination = false;
                    lastKnownPlayerPosition = player != null ? player.position : transform.position;
                    hasLostSight = false;
                    break;
                case EnemyState.Suspicious:
                    suspicionEndsAt = Time.time + suspicionDuration;
                    hasLostSight = false;
                    break;
            }
        }

        private bool TryFindVisibleLight(out LightStimulusSource bestLight)
        {
            bestLight = null;
            float bestDistanceSqr = float.PositiveInfinity;
            foreach (LightStimulusSource light in FindObjectsByType<LightStimulusSource>(FindObjectsSortMode.None))
            {
                if (!light.IsActive || !perception.CanSeePoint(light.Position, true))
                {
                    continue;
                }

                float distanceSqr = (light.Position - transform.position).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestLight = light;
                }
            }

            return bestLight != null;
        }

        private bool TryResolveLightDestination(
            LightStimulusSource light, out Vector3 destination)
        {
            destination = light.NavigationHint;
            if (!agent.isOnNavMesh)
            {
                return false;
            }

            if (NavMesh.SamplePosition(
                    light.NavigationHint, out NavMeshHit hit,
                    lightNavMeshSampleRadius, agent.areaMask))
            {
                destination = hit.position;
                return true;
            }

            return false;
        }

        private bool HasReachedDestination()
        {
            return hasPatrolDestination && agent.isOnNavMesh && !agent.pathPending
                && agent.remainingDistance <= pointArrivalDistance;
        }

        private bool HasReachedPosition(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= pointArrivalDistance * pointArrivalDistance;
        }

        private void Attack()
        {
            ChangeState(EnemyState.Attacking);
            if (enemyRenderer != null)
            {
                enemyRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", Color.red);
                propertyBlock.SetColor("_Color", Color.red);
                enemyRenderer.SetPropertyBlock(propertyBlock);
            }

            gameOverController?.TriggerGameOver();
        }
    }
}
