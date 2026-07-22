using System;
using NHNHackathon.AI;
using UnityEngine;

namespace NHNHackathon.Dance
{
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

        public event Action<bool> BlendStateChanged;

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

            activePlayer = player;
            activeDisguiseState = activePlayer != null
                ? activePlayer.GetComponent<PlayerDisguiseState>()
                : null;
            hasPendingEarlyInput = false;
            SetBlendState(false);

            if (activePlayer != null)
            {
                activePlayer.DanceInputPerformed += HandlePlayerDanceInput;
            }
        }

        private void HandleAIDanceStepChanged(DanceDefinition dance, int stepIndex, float beatTime)
        {
            currentDanceId = dance.Id;
            currentBeatTime = beatTime;
            hasCurrentBeat = true;
            SetBlendState(false);

            if (!hasPendingEarlyInput)
            {
                return;
            }

            bool isWithinWindow = Mathf.Abs(pendingInputTime - beatTime) <= EffectiveTolerance;
            SetBlendState(isWithinWindow && pendingDanceId == currentDanceId && activePlayer != null);
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
                SetBlendState(danceId == currentDanceId);
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
