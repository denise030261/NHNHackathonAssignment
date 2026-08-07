using System;
using NHNHackathon.AI;
using UnityEngine;

namespace NHNHackathon.Dance
{
    public readonly struct DanceStepJudgement
    {
        public DanceStepJudgement(
            bool succeeded, int expectedDanceId, int playerDanceId,
            float beatTime, float beatDuration)
        {
            Succeeded = succeeded;
            ExpectedDanceId = expectedDanceId;
            PlayerDanceId = playerDanceId;
            BeatTime = beatTime;
            BeatDuration = beatDuration;
        }

        public bool Succeeded { get; }
        public int ExpectedDanceId { get; }
        public int PlayerDanceId { get; }
        public float BeatTime { get; }
        public float BeatDuration { get; }
    }

    [DisallowMultipleComponent]
    public sealed class DanceSyncJudge : MonoBehaviour
    {
        [Header("Dance AI")]
        [SerializeField] private DanceSequenceController danceAI;

        [Header("Judgement")]
        [SerializeField, Min(0f), Tooltip("Allowed early or late input time in seconds.")]
        private float timingTolerance = 0.5f;

        private PlayerDanceInput activePlayer;
        private PlayerDisguiseState activeDisguiseState;
        private int currentDanceId;
        private float currentBeatTime = float.NegativeInfinity;
        private bool hasCurrentBeat;
        private bool hasPendingEarlyInput;
        private int pendingDanceId;
        private float pendingInputTime;
        private bool isBlendingIn;
        private bool currentBeatJudged;

        public event Action<bool> BlendStateChanged;
        public event Action<DanceStepJudgement> DanceStepJudged;

        public bool IsBlendingIn => isBlendingIn;
        public float TimingTolerance => timingTolerance;

        private void OnEnable()
        {
            if (danceAI != null)
            {
                danceAI.DanceStepChanged += HandleAIDanceStepChanged;
            }
        }

        private void OnDisable()
        {
            if (danceAI != null)
            {
                danceAI.DanceStepChanged -= HandleAIDanceStepChanged;
            }

            SetActivePlayer(null);
        }

        public void SetActivePlayer(PlayerDanceInput player)
        {
            if (activePlayer == player)
            {
                return;
            }

            if (activePlayer != null)
            {
                activePlayer.DanceInputPerformed -= HandlePlayerDanceInput;
            }

            // Clear the previous player's disguise before replacing its reference.
            // Otherwise leaving the zone while synchronized can leave IsDisguised true forever.
            if (activeDisguiseState != null)
            {
                activeDisguiseState.SetDisguised(false);
            }
            SetBlendState(false);

            activePlayer = player;
            activeDisguiseState = activePlayer != null
                ? activePlayer.GetComponent<PlayerDisguiseState>()
                : null;
            hasPendingEarlyInput = false;

            if (activePlayer != null)
            {
                activePlayer.DanceInputPerformed += HandlePlayerDanceInput;
            }
        }

        private void HandleAIDanceStepChanged(DanceDefinition dance, int stepIndex, float beatTime)
        {
            if (hasCurrentBeat && !currentBeatJudged)
            {
                PublishJudgement(false, -1);
            }

            currentDanceId = dance.Id;
            currentBeatTime = beatTime;
            hasCurrentBeat = true;
            currentBeatJudged = false;
            SetBlendState(false);

            if (!hasPendingEarlyInput)
            {
                return;
            }

            bool isWithinWindow = Mathf.Abs(pendingInputTime - beatTime) <= EffectiveTolerance;
            JudgeCurrentBeat(
                isWithinWindow && pendingDanceId == currentDanceId && activePlayer != null,
                pendingDanceId);
            hasPendingEarlyInput = false;
        }

        private void HandlePlayerDanceInput(int danceId, float inputTime)
        {
            if (activePlayer == null || danceAI == null)
            {
                return;
            }

            float currentDistance = hasCurrentBeat
                ? Mathf.Abs(inputTime - currentBeatTime)
                : float.PositiveInfinity;
            float nextDistance = Mathf.Abs(danceAI.NextBeatTime - inputTime);

            if (nextDistance < currentDistance && nextDistance <= EffectiveTolerance)
            {
                hasPendingEarlyInput = true;
                pendingDanceId = danceId;
                pendingInputTime = inputTime;
                return;
            }

            if (currentDistance <= EffectiveTolerance)
            {
                hasPendingEarlyInput = false;
                JudgeCurrentBeat(danceId == currentDanceId, danceId);
                return;
            }

            if (danceAI.TryGetNextDance(out _) && nextDistance <= EffectiveTolerance)
            {
                hasPendingEarlyInput = true;
                pendingDanceId = danceId;
                pendingInputTime = inputTime;
                return;
            }

            hasPendingEarlyInput = false;
            SetBlendState(false);
            if (hasCurrentBeat && !currentBeatJudged)
            {
                JudgeCurrentBeat(false, danceId);
            }
        }

        private void JudgeCurrentBeat(bool succeeded, int playerDanceId)
        {
            if (!hasCurrentBeat || currentBeatJudged)
            {
                return;
            }

            currentBeatJudged = true;
            SetBlendState(succeeded);
            PublishJudgement(succeeded, playerDanceId);
        }

        private void PublishJudgement(bool succeeded, int playerDanceId)
        {
            DanceStepJudged?.Invoke(new DanceStepJudgement(
                succeeded,
                currentDanceId,
                playerDanceId,
                currentBeatTime,
                danceAI != null ? danceAI.BeatInterval : 0f));
        }

        private float EffectiveTolerance => Mathf.Min(timingTolerance, danceAI.BeatInterval * 0.5f);

        private void SetBlendState(bool value)
        {
            if (isBlendingIn == value)
            {
                return;
            }

            isBlendingIn = value;
            if (activeDisguiseState != null)
            {
                activeDisguiseState.SetDisguised(value);
            }
            BlendStateChanged?.Invoke(isBlendingIn);
        }

        private void OnValidate()
        {
            timingTolerance = Mathf.Max(0f, timingTolerance);
        }
    }
}
