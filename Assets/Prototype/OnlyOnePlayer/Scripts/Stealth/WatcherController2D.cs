using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Core;
using OnlyOnePlayer.Prototype.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class WatcherController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private Collider2D characterCollider;
        [SerializeField] private CharacterStatus characterStatus;
        [SerializeField] private CharacterIdentity characterIdentity;
        [SerializeField] private Transform patrolPointA;
        [SerializeField] private Transform patrolPointB;
        [SerializeField] private PrototypeGameOverHandler gameOverHandler;
        [SerializeField] private RealPlayerIdentity realPlayerTarget;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float patrolSpeed = 1.75f;
        [SerializeField, Min(0f)] private float chaseSpeed = 4f;
        [SerializeField, Min(0.01f)] private float arriveDistance = 0.1f;
        [SerializeField, Min(0.01f)] private float catchDistance = 0.45f;

        [Header("Pursuit")]
        [SerializeField, Min(0f)] private float pursuitRadius = 7f;

        [Header("Camera Bounds")]
        [SerializeField] private bool keepInsideCameraView = true;
        [SerializeField, Min(0f)] private float cameraBoundsPadding = 0.5f;

        [Header("Vision")]
        [SerializeField, Min(0f)] private float viewRadius = 4f;
        [SerializeField, Range(1f, 360f)] private float viewAngle = 90f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private bool requireLineOfSightToStartChase = true;
        [SerializeField] private bool chaseRealPlayerOnSight = true;

        [Header("Test")]
        [SerializeField] private bool chaseOnStartForTest;
        [SerializeField] private RealPlayerIdentity testChaseTarget;

        [Header("Debug")]
        [SerializeField] private bool drawVisionGizmos = true;
        [SerializeField] private Color patrolGizmoColor = Color.cyan;
        [SerializeField] private Color visionGizmoColor = new Color(1f, 0.85f, 0.1f, 0.35f);
        [SerializeField] private Color chaseGizmoColor = Color.red;

        private CharacterIdentity chaseTarget;
        private Transform currentPatrolTarget;
        private Vector2 facingDirection = Vector2.right;
        private readonly Queue<CharacterIdentity> suspiciousTargets = new();
        private readonly HashSet<CharacterIdentity> ignoredNpcTargets = new();
        private readonly HashSet<CharacterIdentity> queuedTargets = new();

        public WatcherState CurrentState { get; private set; } = WatcherState.Patrol;

        public void ReportRuleViolation(RealPlayerIdentity target)
        {
            if (target == null)
            {
                return;
            }

            ReportForbiddenAction(target.CharacterIdentity, ForbiddenActionType.StayingAtTerminalTooLong);
        }

        public void ReportForbiddenAction(CharacterIdentity actor, ForbiddenActionType actionType)
        {
            if (actor == null || CurrentState == WatcherState.GameOver || actor == characterIdentity)
            {
                return;
            }

            if (ignoredNpcTargets.Contains(actor) || !IsInsidePursuitArea(actor))
            {
                return;
            }

            if (chaseTarget == null)
            {
                chaseTarget = actor;
                CurrentState = WatcherState.Chase;
                return;
            }

            if (actor == chaseTarget || queuedTargets.Contains(actor))
            {
                return;
            }

            suspiciousTargets.Enqueue(actor);
            queuedTargets.Add(actor);
        }

        public bool CanSeeTarget(RealPlayerIdentity target)
        {
            if (target == null)
            {
                return false;
            }

            Vector2 origin = transform.position;
            Vector2 targetPosition = target.TargetTransform.position;
            Vector2 directionToTarget = targetPosition - origin;
            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget > viewRadius)
            {
                return false;
            }

            if (distanceToTarget <= Mathf.Epsilon)
            {
                return true;
            }

            float angleToTarget = Vector2.Angle(facingDirection, directionToTarget);
            if (angleToTarget > viewAngle * 0.5f)
            {
                return false;
            }

            RaycastHit2D hit = Physics2D.Raycast(origin, directionToTarget.normalized, distanceToTarget, obstacleMask);
            return hit.collider == null;
        }

        private void Reset()
        {
            targetRigidbody = GetComponent<Rigidbody2D>();
            characterCollider = GetComponent<Collider2D>();
            characterStatus = GetComponent<CharacterStatus>();
            characterIdentity = GetComponent<CharacterIdentity>();
        }

        private void Awake()
        {
            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponent<Rigidbody2D>();
            }

            if (targetRigidbody == null)
            {
                targetRigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            if (characterCollider == null)
            {
                characterCollider = GetComponent<Collider2D>();
            }

            if (characterCollider == null)
            {
                characterCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            characterCollider.isTrigger = false;

            if (characterStatus == null)
            {
                characterStatus = GetComponent<CharacterStatus>();
            }

            if (characterStatus == null)
            {
                characterStatus = gameObject.AddComponent<CharacterStatus>();
            }

            if (characterIdentity == null)
            {
                characterIdentity = GetComponent<CharacterIdentity>();
            }

            if (characterIdentity == null)
            {
                characterIdentity = gameObject.AddComponent<CharacterIdentity>();
            }

            if (characterIdentity.ActorType == CharacterActorType.Unknown)
            {
                characterIdentity.Configure(CharacterActorType.Watcher, name);
            }

            targetRigidbody.gravityScale = 0f;
            targetRigidbody.freezeRotation = true;
            CharacterCollisionRegistry2D.Register(characterCollider);
            currentPatrolTarget = patrolPointB != null ? patrolPointB : patrolPointA;
        }

        private void OnDestroy()
        {
            CharacterCollisionRegistry2D.Unregister(characterCollider);
        }

        private void Start()
        {
            if (realPlayerTarget == null)
            {
                realPlayerTarget = FindAnyObjectByType<RealPlayerIdentity>();
            }

            if (chaseOnStartForTest && testChaseTarget != null)
            {
                chaseTarget = testChaseTarget.CharacterIdentity;
                CurrentState = WatcherState.Chase;
            }
        }

        private void FixedUpdate()
        {
            if (CurrentState == WatcherState.GameOver)
            {
                return;
            }

            if (characterStatus != null && characterStatus.IsStunned)
            {
                targetRigidbody.linearVelocity = Vector2.zero;
                return;
            }

            if (gameOverHandler != null && gameOverHandler.IsGameOver)
            {
                CurrentState = WatcherState.GameOver;
                return;
            }

            if (CurrentState == WatcherState.Chase && chaseTarget != null)
            {
                MoveToward(chaseTarget.TargetTransform.position, chaseSpeed);
                TryVerifyTarget();
                return;
            }

            TryTakeNextSuspiciousTarget();
            if (CurrentState == WatcherState.Chase && chaseTarget != null)
            {
                return;
            }

            TryDetectRealPlayer();
            if (CurrentState == WatcherState.Chase && chaseTarget != null)
            {
                return;
            }

            Patrol();
        }

        private void TryDetectRealPlayer()
        {
            if (!chaseRealPlayerOnSight || realPlayerTarget == null)
            {
                return;
            }

            if (CanSeeTarget(realPlayerTarget))
            {
                chaseTarget = realPlayerTarget.CharacterIdentity;
                CurrentState = WatcherState.Chase;
            }
        }

        private void TryTakeNextSuspiciousTarget()
        {
            while (suspiciousTargets.Count > 0)
            {
                CharacterIdentity nextTarget = suspiciousTargets.Dequeue();
                queuedTargets.Remove(nextTarget);

                if (nextTarget == null || ignoredNpcTargets.Contains(nextTarget) || !IsInsidePursuitArea(nextTarget))
                {
                    continue;
                }

                chaseTarget = nextTarget;
                CurrentState = WatcherState.Chase;
                return;
            }
        }

        private void Patrol()
        {
            if (patrolPointA == null || patrolPointB == null)
            {
                return;
            }

            if (currentPatrolTarget == null)
            {
                currentPatrolTarget = patrolPointB;
            }

            MoveToward(currentPatrolTarget.position, patrolSpeed);

            if (Vector2.Distance(transform.position, currentPatrolTarget.position) <= arriveDistance)
            {
                currentPatrolTarget = currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;
            }
        }

        private void MoveToward(Vector2 targetPosition, float speed)
        {
            Vector2 currentPosition = targetRigidbody.position;
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, speed * Time.fixedDeltaTime);

            if (keepInsideCameraView)
            {
                nextPosition = CameraBoundsUtility2D.ClampToMainCamera(nextPosition, cameraBoundsPadding);
            }

            Vector2 movement = nextPosition - currentPosition;

            if (movement.sqrMagnitude > 0.0001f)
            {
                facingDirection = movement.normalized;
            }

            targetRigidbody.MovePosition(nextPosition);
        }

        private void TryVerifyTarget()
        {
            if (chaseTarget == null)
            {
                return;
            }

            if (Vector2.Distance(transform.position, chaseTarget.TargetTransform.position) <= catchDistance)
            {
                if (chaseTarget.IsRealPlayer)
                {
                    CurrentState = WatcherState.GameOver;
                    gameOverHandler?.GameOver();
                    return;
                }

                ignoredNpcTargets.Add(chaseTarget);
                chaseTarget = null;
                CurrentState = WatcherState.Patrol;
                TryTakeNextSuspiciousTarget();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryGameOverOnTouch(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryGameOverOnTouch(other.gameObject);
        }

        private void TryGameOverOnTouch(GameObject other)
        {
            if (gameOverHandler == null || gameOverHandler.IsGameOver)
            {
                return;
            }

            CharacterIdentity touchedCharacter = other.GetComponent<CharacterIdentity>();
            if (touchedCharacter == null || !touchedCharacter.IsRealPlayer)
            {
                return;
            }

            chaseTarget = touchedCharacter;
            CurrentState = WatcherState.GameOver;
            gameOverHandler.GameOver();
        }

        private bool IsInsidePursuitArea(CharacterIdentity actor)
        {
            return actor != null && Vector2.Distance(transform.position, actor.TargetTransform.position) <= pursuitRadius;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawVisionGizmos)
            {
                return;
            }

            Vector3 position = transform.position;
            Vector2 gizmoForward = facingDirection.sqrMagnitude > 0f ? facingDirection : Vector2.right;
            Vector3 leftBoundary = DirectionFromAngle(-viewAngle * 0.5f, gizmoForward) * viewRadius;
            Vector3 rightBoundary = DirectionFromAngle(viewAngle * 0.5f, gizmoForward) * viewRadius;

            Gizmos.color = visionGizmoColor;
            Gizmos.DrawLine(position, position + leftBoundary);
            Gizmos.DrawLine(position, position + rightBoundary);
            Gizmos.DrawWireSphere(position, viewRadius);

            if (patrolPointA != null && patrolPointB != null)
            {
                Gizmos.color = patrolGizmoColor;
                Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);
            }

            if (CurrentState == WatcherState.Chase && chaseTarget != null)
            {
                Gizmos.color = chaseGizmoColor;
                Gizmos.DrawLine(position, chaseTarget.TargetTransform.position);
            }

            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(position, pursuitRadius);
        }

        private static Vector3 DirectionFromAngle(float angleOffset, Vector2 forward)
        {
            float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float angle = (baseAngle + angleOffset) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }
    }
}
