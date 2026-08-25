using NHNHackathon.Dance;
using NHNHackathon.Game;
using NHNHackathon.LightSystem;
using NHNHackathon.ExitSystem;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    public sealed class EnemyController : MonoBehaviour
    {
        private enum ScriptedSuspicionArrivalMode
        {
            TimedSuspicion,
            ResumeRoaming,
            HoldSuspicion
        }

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerDisguiseState playerDisguise;
        [SerializeField] private EnemyPatrolRoute patrolRoute;
        [SerializeField] private GameOverController gameOverController;

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
        [SerializeField, Min(0.1f), Tooltip("Radius used to place a scripted suspicion target on the NavMesh.")]
        private float scriptedSuspicionNavMeshSampleRadius = 2f;

        [Header("Final Dance Watch")]
        [SerializeField, Min(0f), Tooltip("How long a visible player may remain out of sync before the watcher starts chasing.")]
        private float danceWatchFailureGraceDuration = 0.75f;
        [SerializeField, Min(0f), Tooltip("Horizontal movement detected per perception check that immediately breaks the dance disguise.")]
        private float danceWatchMovementThreshold = 0.05f;

        [Header("Performance")]
        [SerializeField, Min(0.02f)] private float perceptionInterval = 0.1f;
        [SerializeField, Min(0.02f)] private float lightSearchInterval = 0.1f;
        [SerializeField, Min(0.02f)] private float chaseDestinationUpdateInterval = 0.1f;
        [SerializeField, Min(0f)] private float chaseDestinationChangeThreshold = 0.15f;

        private NavMeshAgent agent;
        private EnemyPerception perception;
        private int patrolIndex;
        private float waitUntil;
        private float nextPerceptionTime;
        private float nextLightSearchTime;
        private float nextChaseDestinationUpdate;
        private Vector3 lastChaseDestination;
        private float suspicionEndsAt;
        private float investigationStartedAt;
        private float nextLightDestinationUpdate;
        private float lightLostAt;
        private bool hasPatrolDestination;
        private Vector3 lastKnownLightPosition;
        private Vector3 lastKnownPlayerPosition;
        private bool hasLostSight;
        private float lostSightStartedAt;
        private EnemyPatrolRoute pendingPatrolRoute;
        private PatrolRouteStartMode pendingPatrolStartMode;
        private bool hasPendingPatrolRoute;
        private bool hasScriptedSuspicionDestination;
        private Vector3 scriptedSuspicionDestination;
        private ScriptedSuspicionArrivalMode scriptedSuspicionArrivalMode;
        private bool hasPersistentSuspicion;
        private Vector3 persistentSuspicionDestination;
        private float danceWatchFailureStartedAt = -1f;
        private bool hasDanceWatchPlayerPosition;
        private Vector3 lastDanceWatchPlayerPosition;

        public EnemyState CurrentState { get; private set; }
        public EnemyPatrolRoute PatrolRoute => patrolRoute;
        public event System.Action ScriptedSuspicionDestinationReached;

        public void ResumeAfterCutscene()
        {
            agent ??= GetComponent<NavMeshAgent>();
            perception ??= GetComponent<EnemyPerception>();
            hasPatrolDestination = false;
            hasLostSight = false;
            hasPendingPatrolRoute = false;
            waitUntil = Time.time;
            nextPerceptionTime = 0f;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }

            ChangeState(EnemyState.Roaming);
            EvaluatePlayer();
        }

        public void SetPatrolRoute(
            EnemyPatrolRoute route, PatrolRouteStartMode startMode = PatrolRouteStartMode.NearestPoint)
        {
            if (CurrentState != EnemyState.Roaming)
            {
                pendingPatrolRoute = route;
                pendingPatrolStartMode = startMode;
                hasPendingPatrolRoute = true;
                return;
            }

            ApplyPatrolRoute(route, startMode);
        }

        public void AlertToPlayer(Transform alertedPlayer)
        {
            if (DeveloperModeController.ShouldWatchersIgnorePlayer
                || alertedPlayer == null || CurrentState == EnemyState.Attacking)
            {
                return;
            }

            perception ??= GetComponent<EnemyPerception>();
            if ((hasPersistentSuspicion || CurrentState == EnemyState.WatchingDance)
                && (perception == null || !perception.CanSeeTarget(alertedPlayer)))
            {
                return;
            }

            player = alertedPlayer;
            playerDisguise = player.GetComponent<PlayerDisguiseState>();
            lastKnownPlayerPosition = player.position;
            hasLostSight = false;
            hasScriptedSuspicionDestination = false;
            hasPersistentSuspicion = false;
            ChangeState(EnemyState.Chasing);
        }

        public void BeginDanceWatch()
        {
            hasScriptedSuspicionDestination = false;
            hasPersistentSuspicion = false;
            ChangeState(EnemyState.WatchingDance);
        }

        public bool MoveSuspiciouslyTo(Transform target)
        {
            return BeginScriptedSuspicionMovement(
                target, ScriptedSuspicionArrivalMode.TimedSuspicion);
        }

        public bool MoveSuspiciouslyToAndResumePatrol(
            Transform target, EnemyPatrolRoute route,
            PatrolRouteStartMode startMode = PatrolRouteStartMode.NearestPoint)
        {
            if (!BeginScriptedSuspicionMovement(
                    target, ScriptedSuspicionArrivalMode.ResumeRoaming))
            {
                return false;
            }

            SetPatrolRoute(route, startMode);
            return true;
        }

        public bool MoveSuspiciouslyToAndHold(Transform target)
        {
            return BeginScriptedSuspicionMovement(
                target, ScriptedSuspicionArrivalMode.HoldSuspicion);
        }

        private bool BeginScriptedSuspicionMovement(
            Transform target, ScriptedSuspicionArrivalMode arrivalMode)
        {
            if (target == null)
            {
                return false;
            }

            agent ??= GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh
                || !NavMesh.SamplePosition(
                    target.position, out NavMeshHit hit,
                    Mathf.Max(0.1f, scriptedSuspicionNavMeshSampleRadius), agent.areaMask))
            {
                return false;
            }

            scriptedSuspicionDestination = hit.position;
            scriptedSuspicionArrivalMode = arrivalMode;
            hasPersistentSuspicion = arrivalMode
                == ScriptedSuspicionArrivalMode.HoldSuspicion;
            if (hasPersistentSuspicion)
            {
                persistentSuspicionDestination = hit.position;
            }
            hasScriptedSuspicionDestination = true;
            ChangeState(EnemyState.Suspicious);
            return hasScriptedSuspicionDestination;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<EnemyPerception>();
        }

        private void OnEnable()
        {
            DoorNavigationController.NavigationChanged += RefreshNavigationPath;
        }

        private void OnDisable()
        {
            DoorNavigationController.NavigationChanged -= RefreshNavigationPath;
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

            if (DeveloperModeController.ShouldWatchersIgnorePlayer
                && !hasScriptedSuspicionDestination
                && !hasPersistentSuspicion
                && CurrentState is EnemyState.Chasing
                    or EnemyState.Suspicious
                    or EnemyState.Attacking)
            {
                ChangeState(EnemyState.Roaming);
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
            if (hasScriptedSuspicionDestination
                || hasPersistentSuspicion
                || DeveloperModeController.ShouldWatchersIgnorePlayer
                || player == null || CurrentState == EnemyState.Attacking)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            bool isDisguised = playerDisguise != null && playerDisguise.IsDisguised;

            switch (CurrentState)
            {
                case EnemyState.Roaming:
                    EvaluatePlayerWhileRoaming(isDisguised);
                    break;
                case EnemyState.Chasing:
                    EvaluatePlayerWhileChasing(isDisguised, distance);
                    break;
                case EnemyState.Suspicious:
                    EvaluatePlayerWhileSuspicious(isDisguised, distance);
                    break;
                case EnemyState.WatchingDance:
                    EvaluatePlayerWhileWatchingDance(isDisguised);
                    break;
            }
        }

        private void EvaluatePlayerWhileWatchingDance(bool isDisguised)
        {
            Vector3 currentPosition = player.position;
            Vector3 movement = hasDanceWatchPlayerPosition
                ? currentPosition - lastDanceWatchPlayerPosition
                : Vector3.zero;
            movement.y = 0f;
            lastDanceWatchPlayerPosition = currentPosition;
            hasDanceWatchPlayerPosition = true;

            if (!perception.CanSeeTarget(player))
            {
                danceWatchFailureStartedAt = -1f;
                return;
            }

            float movementThresholdSquared = danceWatchMovementThreshold
                * danceWatchMovementThreshold;
            if (movement.sqrMagnitude > movementThresholdSquared)
            {
                ChangeState(EnemyState.Chasing);
                return;
            }

            if (isDisguised)
            {
                danceWatchFailureStartedAt = -1f;
                return;
            }

            if (danceWatchFailureStartedAt < 0f)
            {
                danceWatchFailureStartedAt = Time.time;
                return;
            }

            if (Time.time - danceWatchFailureStartedAt
                >= danceWatchFailureGraceDuration)
            {
                ChangeState(EnemyState.Chasing);
            }
        }

        private void EvaluatePlayerWhileRoaming(bool isDisguised)
        {
            // A valid disguise is always trusted until this enemy has already begun a chase.
            if (isDisguised)
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
            if (isDisguised && distance >= minimumDisguiseDistance)
            {
                return;
            }

            if (perception.CanSeeTarget(player))
            {
                ChangeState(EnemyState.Chasing);
            }
        }

        public void TryCapturePlayer(Transform candidate)
        {
            if (hasScriptedSuspicionDestination
                || hasPersistentSuspicion
                || DeveloperModeController.ShouldWatchersIgnorePlayer)
            {
                return;
            }

            bool isPlayerTransform = candidate != null && player != null
                && (candidate == player || candidate.IsChildOf(player) || player.IsChildOf(candidate));
            if (!isPlayerTransform || CurrentState == EnemyState.Attacking)
            {
                return;
            }

            bool isDisguised = playerDisguise != null && playerDisguise.IsDisguised;
            bool disguiseIsTrusted = isDisguised
                && CurrentState is EnemyState.Roaming or EnemyState.WatchingDance;
            if (!disguiseIsTrusted)
            {
                Attack();
            }
        }

        private void UpdateRoaming()
        {

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

        private void UpdateChasing()
        {
            if (player == null || !agent.isOnNavMesh
                || Time.time < nextChaseDestinationUpdate)
            {
                return;
            }

            nextChaseDestinationUpdate = Time.time
                + chaseDestinationUpdateInterval;
            Vector3 destination = hasLostSight
                ? lastKnownPlayerPosition
                : player.position;
            float thresholdSquared = chaseDestinationChangeThreshold
                * chaseDestinationChangeThreshold;
            if (!agent.hasPath
                || (destination - lastChaseDestination).sqrMagnitude
                >= thresholdSquared)
            {
                agent.SetDestination(destination);
                lastChaseDestination = destination;
            }
        }

        private void UpdateSuspicious()
        {
            if (hasScriptedSuspicionDestination)
            {
                UpdateScriptedSuspicionMovement();
                return;
            }

            if (hasPersistentSuspicion)
            {
                return;
            }

            if (Time.time >= suspicionEndsAt)
            {
                ChangeState(EnemyState.Roaming);
            }
        }

        private void UpdateScriptedSuspicionMovement()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                CancelScriptedSuspicionMovement();
                return;
            }

            if (HasReachedPosition(scriptedSuspicionDestination))
            {
                CompleteScriptedSuspicionMovement();
                return;
            }

            if (!agent.pathPending && !agent.hasPath
                && !agent.SetDestination(scriptedSuspicionDestination))
            {
                CancelScriptedSuspicionMovement();
            }
        }

        private void CompleteScriptedSuspicionMovement()
        {
            ScriptedSuspicionArrivalMode arrivalMode = scriptedSuspicionArrivalMode;
            hasScriptedSuspicionDestination = false;
            StopAgentAtCurrentPosition();

            switch (arrivalMode)
            {
                case ScriptedSuspicionArrivalMode.ResumeRoaming:
                    ChangeState(EnemyState.Roaming);
                    break;
                case ScriptedSuspicionArrivalMode.HoldSuspicion:
                    hasPersistentSuspicion = true;
                    suspicionEndsAt = float.PositiveInfinity;
                    break;
                default:
                    suspicionEndsAt = Time.time + suspicionDuration;
                    break;
            }

            ScriptedSuspicionDestinationReached?.Invoke();
        }

        private void CancelScriptedSuspicionMovement()
        {
            hasScriptedSuspicionDestination = false;
            hasPersistentSuspicion = false;
            suspicionEndsAt = Time.time + suspicionDuration;
            StopAgentAtCurrentPosition();
        }

        private void StopAgentAtCurrentPosition()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private void RefreshNavigationPath()
        {
            hasPatrolDestination = false;
            nextLightDestinationUpdate = 0f;
            waitUntil = Time.time;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }

        private void ChangeState(EnemyState newState)
        {
            if (newState != EnemyState.Suspicious)
            {
                hasScriptedSuspicionDestination = false;
            }

            if (newState == EnemyState.Roaming)
            {
                hasPersistentSuspicion = false;
            }
            else if (newState == EnemyState.Suspicious
                && hasPersistentSuspicion
                && !HasReachedPosition(persistentSuspicionDestination))
            {
                scriptedSuspicionDestination = persistentSuspicionDestination;
                scriptedSuspicionArrivalMode =
                    ScriptedSuspicionArrivalMode.HoldSuspicion;
                hasScriptedSuspicionDestination = true;
            }

            CurrentState = newState;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = newState == EnemyState.Attacking
                    || newState == EnemyState.WatchingDance
                    || (newState == EnemyState.Suspicious
                        && !hasScriptedSuspicionDestination);
                if (agent.isStopped)
                {
                    agent.ResetPath();
                }
            }

            switch (newState)
            {
                case EnemyState.Roaming:
                    if (hasPendingPatrolRoute)
                    {
                        ApplyPatrolRoute(pendingPatrolRoute, pendingPatrolStartMode);
                        hasPendingPatrolRoute = false;
                    }
                    agent.speed = patrolSpeed;
                    hasPatrolDestination = false;
                    waitUntil = Time.time;
                    break;
                case EnemyState.Chasing:
                    agent.speed = chaseSpeed;
                    hasPatrolDestination = false;
                    lastKnownPlayerPosition = player != null ? player.position : transform.position;
                    hasLostSight = false;
                    nextChaseDestinationUpdate = 0f;
                    lastChaseDestination = new Vector3(
                        float.PositiveInfinity, 0f, 0f);
                    break;
                case EnemyState.Suspicious:
                    agent.speed = patrolSpeed;
                    suspicionEndsAt = hasScriptedSuspicionDestination
                        ? float.PositiveInfinity
                        : Time.time + suspicionDuration;
                    hasLostSight = false;
                    if (hasScriptedSuspicionDestination
                        && !agent.SetDestination(scriptedSuspicionDestination))
                    {
                        CancelScriptedSuspicionMovement();
                    }
                    break;
                case EnemyState.WatchingDance:
                    agent.speed = patrolSpeed;
                    danceWatchFailureStartedAt = -1f;
                    hasDanceWatchPlayerPosition = player != null;
                    if (hasDanceWatchPlayerPosition)
                    {
                        lastDanceWatchPlayerPosition = player.position;
                    }
                    break;
            }
        }

        private void ApplyPatrolRoute(EnemyPatrolRoute route, PatrolRouteStartMode startMode)
        {
            patrolRoute = route;
            if (route == null || route.Count == 0)
            {
                patrolIndex = 0;
            }
            else
            {
                patrolIndex = startMode switch
                {
                    PatrolRouteStartMode.FirstPoint => 0,
                    PatrolRouteStartMode.KeepCurrentIndex => patrolIndex % route.Count,
                    _ => route.FindNearestPointIndex(transform.position)
                };
            }

            hasPatrolDestination = false;
            waitUntil = Time.time;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
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
            gameOverController?.TriggerGameOver(this);
        }
    }
}
