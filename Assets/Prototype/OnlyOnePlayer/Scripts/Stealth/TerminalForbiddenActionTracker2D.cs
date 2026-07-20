using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Mission;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class TerminalForbiddenActionTracker2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ForbiddenActionReporter reporter;
        [SerializeField] private DataExtractionPoint2D dataPoint;
        [SerializeField] private Collider2D triggerCollider;

        [Header("Staying Rule")]
        [SerializeField, Min(0f)] private float maxStaySeconds = 5f;

        [Header("Repeated Approach Rule")]
        [SerializeField, Min(1)] private int repeatedApproachLimit = 5;
        [SerializeField, Min(0f)] private float approachResetSeconds = 3f;

        [Header("Leave After Hack Rule")]
        [SerializeField, Min(0f)] private float leaveAfterHackWindow = 1f;

        private readonly Dictionary<CharacterIdentity, float> stayTimers = new();
        private readonly Dictionary<CharacterIdentity, int> approachCounts = new();
        private readonly Dictionary<CharacterIdentity, float> lastApproachTimes = new();
        private readonly Dictionary<CharacterIdentity, float> hackCompletedTimes = new();
        private readonly HashSet<CharacterIdentity> reportedStayers = new();

        private void Awake()
        {
            if (reporter == null)
            {
                reporter = FindAnyObjectByType<ForbiddenActionReporter>();
            }

            if (dataPoint == null)
            {
                dataPoint = GetComponent<DataExtractionPoint2D>();
            }

            SetupTrigger();
        }

        private void OnEnable()
        {
            SetupTrigger();

            if (dataPoint != null)
            {
                dataPoint.Collected += HandleDataCollected;
            }
        }

        private void OnDisable()
        {
            if (dataPoint != null)
            {
                dataPoint.Collected -= HandleDataCollected;
            }
        }

        private void Update()
        {
            if (stayTimers.Count == 0)
            {
                return;
            }

            CharacterIdentity[] actors = new CharacterIdentity[stayTimers.Count];
            stayTimers.Keys.CopyTo(actors, 0);

            foreach (CharacterIdentity actor in actors)
            {
                if (actor == null)
                {
                    stayTimers.Remove(actor);
                    continue;
                }

                stayTimers[actor] += Time.deltaTime;

                if (stayTimers[actor] >= maxStaySeconds && !reportedStayers.Contains(actor))
                {
                    reportedStayers.Add(actor);
                    reporter?.Report(actor, ForbiddenActionType.StayingAtTerminalTooLong);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            stayTimers[actor] = 0f;
            UpdateApproachCount(actor);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            stayTimers.Remove(actor);
            reportedStayers.Remove(actor);

            if (hackCompletedTimes.TryGetValue(actor, out float completedTime) &&
                Time.time - completedTime <= leaveAfterHackWindow)
            {
                reporter?.Report(actor, ForbiddenActionType.LeavingImmediatelyAfterHack);
            }
        }

        private void UpdateApproachCount(CharacterIdentity actor)
        {
            float now = Time.time;
            if (!lastApproachTimes.TryGetValue(actor, out float lastTime) || now - lastTime > approachResetSeconds)
            {
                approachCounts[actor] = 0;
            }

            lastApproachTimes[actor] = now;
            approachCounts[actor] = approachCounts.TryGetValue(actor, out int count) ? count + 1 : 1;

            if (approachCounts[actor] >= repeatedApproachLimit)
            {
                approachCounts[actor] = 0;
                reporter?.Report(actor, ForbiddenActionType.RepeatedTerminalApproach);
            }
        }

        private void HandleDataCollected(CharacterIdentity actor)
        {
            if (actor != null)
            {
                hackCompletedTimes[actor] = Time.time;
            }
        }

        private void Reset()
        {
            dataPoint = GetComponent<DataExtractionPoint2D>();
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
