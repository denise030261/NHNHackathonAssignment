using System;
using System.Collections.Generic;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Lighting
{
    public enum ConditionMatchMode
    {
        All,
        Any
    }

    [Serializable]
    public sealed class ProgressionRequirement
    {
        [SerializeField] private ProgressionCondition condition;
        [SerializeField, Tooltip("Disable to require this condition to be incomplete.")]
        private bool mustBeCompleted = true;

        public bool IsSatisfied(GameProgressionController progression)
        {
            if (condition == null)
            {
                return false;
            }

            return progression.IsCompleted(condition) == mustBeCompleted;
        }
    }

    [Serializable]
    public struct LightTransitionSettings
    {
        [SerializeField, Min(0f)] private float delay;
        [SerializeField] private bool useFlicker;
        [SerializeField, Min(1)] private int flickerCount;
        [SerializeField, Min(0.01f)] private float flickerInterval;

        public float Delay => delay;
        public bool UseFlicker => useFlicker;
        public int FlickerCount => Mathf.Max(1, flickerCount);
        public float FlickerInterval => Mathf.Max(0.01f, flickerInterval);

        public static LightTransitionSettings Immediate => default;
    }

    [Serializable]
    public sealed class LightingAction
    {
        [SerializeField] private LightingGroup lightingGroup;
        [SerializeField] private bool turnOn = true;
        [SerializeField] private LightTransitionSettings transition;

        public LightingGroup LightingGroup => lightingGroup;
        public bool TurnOn => turnOn;
        public LightTransitionSettings Transition => transition;
    }

    [Serializable]
    public sealed class ProgressionLightingRule
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string ruleName = "Lighting Rule";
        [SerializeField] private ConditionMatchMode conditionMode = ConditionMatchMode.All;
        [SerializeField] private List<ProgressionRequirement> conditions = new();
        [SerializeField] private List<LightingAction> actions = new();

        public bool Enabled => enabled;
        public string RuleName => ruleName;
        public IReadOnlyList<LightingAction> Actions => actions;

        public bool IsMatched(GameProgressionController progression)
        {
            if (!enabled)
            {
                return false;
            }

            if (conditions.Count == 0)
            {
                return true;
            }

            if (conditionMode == ConditionMatchMode.All)
            {
                foreach (ProgressionRequirement condition in conditions)
                {
                    if (condition == null || !condition.IsSatisfied(progression))
                    {
                        return false;
                    }
                }
                return true;
            }

            foreach (ProgressionRequirement condition in conditions)
            {
                if (condition != null && condition.IsSatisfied(progression))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
