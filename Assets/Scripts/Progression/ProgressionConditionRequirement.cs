using System;
using UnityEngine;

namespace NHNHackathon.Progression
{
    public enum ProgressionMatchMode
    {
        All,
        Any
    }

    [Serializable]
    public sealed class ProgressionConditionRequirement
    {
        [SerializeField] private ProgressionCondition condition;
        [SerializeField, Tooltip("Disable to require this condition to be incomplete.")]
        private bool mustBeCompleted = true;

        public bool IsSatisfied(GameProgressionController progression)
        {
            return progression != null
                && condition != null
                && progression.IsCompleted(condition) == mustBeCompleted;
        }
    }
}
