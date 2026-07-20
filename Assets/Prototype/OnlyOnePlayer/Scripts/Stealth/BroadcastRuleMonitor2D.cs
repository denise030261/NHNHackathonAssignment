using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class BroadcastRuleMonitor2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BroadcastSystemController broadcastSystem;
        [SerializeField] private ForbiddenActionReporter reporter;

        [Header("Movement Rule")]
        [SerializeField, Min(0f)] private float minimumMoveDistance = 0.02f;
        [SerializeField, Min(0f)] private float reportCooldownSeconds = 1f;

        private readonly Dictionary<CharacterIdentity, Vector2> previousPositions = new();
        private readonly Dictionary<CharacterIdentity, float> lastReportTimes = new();

        private void Awake()
        {
            if (broadcastSystem == null)
            {
                broadcastSystem = FindAnyObjectByType<BroadcastSystemController>();
            }

            if (reporter == null)
            {
                reporter = FindAnyObjectByType<ForbiddenActionReporter>();
            }
        }

        private void OnEnable()
        {
            if (broadcastSystem != null)
            {
                broadcastSystem.InstructionStateChanged += HandleInstructionStateChanged;
            }
        }

        private void OnDisable()
        {
            if (broadcastSystem != null)
            {
                broadcastSystem.InstructionStateChanged -= HandleInstructionStateChanged;
            }
        }

        private void LateUpdate()
        {
            if (broadcastSystem == null ||
                !broadcastSystem.IsInstructionActive ||
                broadcastSystem.CurrentInstruction != BroadcastInstructionType.FreezeAllExceptWatchers)
            {
                return;
            }

            CharacterIdentity[] actors = FindObjectsByType<CharacterIdentity>(FindObjectsSortMode.None);
            foreach (CharacterIdentity actor in actors)
            {
                if (actor == null || actor.ActorType == CharacterActorType.Watcher)
                {
                    continue;
                }

                Vector2 currentPosition = actor.TargetTransform.position;
                if (!previousPositions.TryGetValue(actor, out Vector2 previousPosition))
                {
                    previousPositions[actor] = currentPosition;
                    continue;
                }

                Vector2 movement = currentPosition - previousPosition;
                previousPositions[actor] = currentPosition;

                if (movement.sqrMagnitude < minimumMoveDistance * minimumMoveDistance || IsReportOnCooldown(actor))
                {
                    continue;
                }

                lastReportTimes[actor] = Time.time;
                reporter?.Report(actor, ForbiddenActionType.DisobeyingBroadcastInstruction);
            }
        }

        private void HandleInstructionStateChanged(BroadcastInstructionType instructionType, bool isActive)
        {
            previousPositions.Clear();

            if (!isActive || instructionType != BroadcastInstructionType.FreezeAllExceptWatchers)
            {
                return;
            }

            CharacterIdentity[] actors = FindObjectsByType<CharacterIdentity>(FindObjectsSortMode.None);
            foreach (CharacterIdentity actor in actors)
            {
                if (actor != null && actor.ActorType != CharacterActorType.Watcher)
                {
                    previousPositions[actor] = actor.TargetTransform.position;
                }
            }
        }

        private bool IsReportOnCooldown(CharacterIdentity actor)
        {
            return lastReportTimes.TryGetValue(actor, out float lastReportTime) &&
                Time.time - lastReportTime < reportCooldownSeconds;
        }
    }
}
