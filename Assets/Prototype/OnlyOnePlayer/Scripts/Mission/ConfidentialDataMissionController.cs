using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Stealth;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Mission
{
    public sealed class ConfidentialDataMissionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RealPlayerIdentity realPlayer;
        [SerializeField] private WatcherController2D[] watchers;

        [Header("Mission")]
        [SerializeField, Min(1)] private int requiredDataCount = 2;

        [Header("Result")]
        [SerializeField] private bool pauseTimeOnWin = true;
        [SerializeField] private string winMessage = "Mission Complete: confidential data escaped.";
        [SerializeField] private string fakeExitMessage = "Fake exit used: watcher is chasing the real player.";

        private readonly HashSet<DataExtractionPoint2D> collectedPoints = new();

        public RealPlayerIdentity RealPlayer => realPlayer;
        public int CollectedDataCount => collectedPoints.Count;
        public bool IsMissionComplete { get; private set; }

        public void RegisterDataCollected(DataExtractionPoint2D point)
        {
            if (point == null || IsMissionComplete || collectedPoints.Contains(point))
            {
                return;
            }

            collectedPoints.Add(point);
            Debug.Log($"Data collected: {CollectedDataCount}/{requiredDataCount}", this);
        }

        public bool HasCollectedAllData()
        {
            return CollectedDataCount >= requiredDataCount;
        }

        public void EnterExit(ExitType exitType)
        {
            if (IsMissionComplete)
            {
                return;
            }

            if (exitType == ExitType.Fake)
            {
                Debug.Log(fakeExitMessage, this);
                return;
            }

            if (!HasCollectedAllData())
            {
                Debug.Log($"Real exit locked: collect {requiredDataCount - CollectedDataCount} more data.", this);
                return;
            }

            Win();
        }

        public void AlertWatchers()
        {
            if (realPlayer == null || watchers == null)
            {
                return;
            }

            foreach (WatcherController2D watcher in watchers)
            {
                if (watcher != null)
                {
                    watcher.ReportRuleViolation(realPlayer);
                }
            }
        }

        private void Awake()
        {
            if (realPlayer == null)
            {
                realPlayer = FindAnyObjectByType<RealPlayerIdentity>();
            }
        }

        private void Win()
        {
            IsMissionComplete = true;
            Debug.Log(winMessage, this);

            if (pauseTimeOnWin)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
