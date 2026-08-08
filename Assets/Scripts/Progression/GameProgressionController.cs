using System;
using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Progression
{
    [DisallowMultipleComponent]
    public sealed class GameProgressionController : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Conditions considered complete when Play Mode begins.")]
        private List<ProgressionCondition> initiallyCompletedConditions = new();

        private readonly HashSet<ProgressionCondition> completedConditions = new();

        public static GameProgressionController Instance { get; private set; }
        public event Action ProgressionChanged;
        public IReadOnlyCollection<ProgressionCondition> CompletedConditions => completedConditions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            foreach (ProgressionCondition condition in initiallyCompletedConditions)
            {
                if (condition != null)
                {
                    completedConditions.Add(condition);
                }
            }
        }

        private void Start()
        {
            ProgressionChanged?.Invoke();
        }

        public bool IsCompleted(ProgressionCondition condition)
        {
            return condition != null && completedConditions.Contains(condition);
        }

        public bool TryComplete(ProgressionCondition condition)
        {
            if (condition == null || !completedConditions.Add(condition))
            {
                return false;
            }

            ProgressionChanged?.Invoke();
            return true;
        }

        public void Restore(IEnumerable<ProgressionCondition> conditions)
        {
            completedConditions.Clear();
            foreach (ProgressionCondition condition in initiallyCompletedConditions)
            {
                if (condition != null)
                {
                    completedConditions.Add(condition);
                }
            }
            if (conditions != null)
            {
                foreach (ProgressionCondition condition in conditions)
                {
                    if (condition != null)
                    {
                        completedConditions.Add(condition);
                    }
                }
            }
            ProgressionChanged?.Invoke();
        }

        [ContextMenu("Clear Runtime Progress")]
        private void ClearRuntimeProgress()
        {
            completedConditions.Clear();
            ProgressionChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
