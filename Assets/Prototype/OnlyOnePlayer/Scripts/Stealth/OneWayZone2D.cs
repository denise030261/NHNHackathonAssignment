using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class OneWayZone2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ForbiddenActionReporter reporter;
        [SerializeField] private Collider2D triggerCollider;

        [Header("Rule")]
        [SerializeField] private OneWayDirection2D allowedDirection = OneWayDirection2D.Right;
        [SerializeField, Range(-1f, 0f)] private float violationDotThreshold = -0.2f;
        [SerializeField, Min(0f)] private float minimumMoveDistance = 0.015f;
        [SerializeField, Min(0f)] private float reportCooldownSeconds = 1f;

        private readonly Dictionary<CharacterIdentity, Vector2> previousPositions = new();
        private readonly Dictionary<CharacterIdentity, float> lastReportTimes = new();

        public OneWayDirection2D AllowedDirection => allowedDirection;
        public Vector2 AllowedVector => allowedDirection.ToVector();

        private void Awake()
        {
            if (reporter == null)
            {
                reporter = FindAnyObjectByType<ForbiddenActionReporter>();
            }

            SetupTrigger();
        }

        private void OnEnable()
        {
            SetupTrigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            previousPositions[actor] = actor.TargetTransform.position;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            Vector2 currentPosition = actor.TargetTransform.position;
            if (!previousPositions.TryGetValue(actor, out Vector2 previousPosition))
            {
                previousPositions[actor] = currentPosition;
                return;
            }

            Vector2 movement = currentPosition - previousPosition;
            previousPositions[actor] = currentPosition;

            if (movement.sqrMagnitude < minimumMoveDistance * minimumMoveDistance)
            {
                return;
            }

            float directionDot = Vector2.Dot(movement.normalized, AllowedVector);
            if (directionDot >= violationDotThreshold || IsReportOnCooldown(actor))
            {
                return;
            }

            lastReportTimes[actor] = Time.time;
            reporter?.Report(actor, ForbiddenActionType.MovingAgainstOneWayZone);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            previousPositions.Remove(actor);
        }

        private bool IsReportOnCooldown(CharacterIdentity actor)
        {
            return lastReportTimes.TryGetValue(actor, out float lastReportTime) &&
                Time.time - lastReportTime < reportCooldownSeconds;
        }

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void SetupTrigger()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            triggerCollider.isTrigger = true;
        }
    }
}
