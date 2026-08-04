using System;
using System.Collections.Generic;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Enemy
{
    public enum PatrolRouteStartMode
    {
        NearestPoint,
        FirstPoint,
        KeepCurrentIndex
    }

    [Serializable]
    public sealed class ProgressionPatrolRule
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string ruleName = "Patrol Rule";
        [SerializeField] private ProgressionMatchMode conditionMode = ProgressionMatchMode.All;
        [SerializeField] private List<ProgressionConditionRequirement> conditions = new();
        [SerializeField] private EnemyPatrolRoute patrolRoute;
        [SerializeField] private PatrolRouteStartMode startMode = PatrolRouteStartMode.NearestPoint;

        public string RuleName => ruleName;
        public EnemyPatrolRoute PatrolRoute => patrolRoute;
        public PatrolRouteStartMode StartMode => startMode;

        public bool IsMatched(GameProgressionController progression)
        {
            if (!enabled || patrolRoute == null)
            {
                return false;
            }

            if (conditions.Count == 0)
            {
                return true;
            }

            if (conditionMode == ProgressionMatchMode.All)
            {
                foreach (ProgressionConditionRequirement requirement in conditions)
                {
                    if (requirement == null || !requirement.IsSatisfied(progression))
                    {
                        return false;
                    }
                }
                return true;
            }

            foreach (ProgressionConditionRequirement requirement in conditions)
            {
                if (requirement != null && requirement.IsSatisfied(progression))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
