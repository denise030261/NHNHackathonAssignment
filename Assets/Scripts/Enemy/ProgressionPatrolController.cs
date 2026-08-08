using System.Collections.Generic;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class ProgressionPatrolController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private GameProgressionController progressionController;

        [Header("Fallback")]
        [SerializeField] private EnemyPatrolRoute defaultPatrolRoute;
        [SerializeField] private PatrolRouteStartMode defaultStartMode =
            PatrolRouteStartMode.NearestPoint;

        [Header("Rules")]
        [SerializeField, Tooltip("Evaluated top to bottom. The last matching rule wins.")]
        private List<ProgressionPatrolRule> rules = new();

        private EnemyPatrolRoute lastRoute;
        private PatrolRouteStartMode lastStartMode;

        private void Awake()
        {
            enemyController ??= GetComponent<EnemyController>();
        }

        private void Start()
        {
            progressionController ??= GameProgressionController.Instance;
            if (progressionController != null)
            {
                progressionController.ProgressionChanged += EvaluateRules;
            }
            EvaluateRules();
        }

        private void OnDestroy()
        {
            if (progressionController != null)
            {
                progressionController.ProgressionChanged -= EvaluateRules;
            }
        }

        [ContextMenu("Evaluate Patrol Rules")]
        public void EvaluateRules()
        {
            if (enemyController == null || progressionController == null)
            {
                return;
            }

            EnemyPatrolRoute resolvedRoute = defaultPatrolRoute != null
                ? defaultPatrolRoute
                : enemyController.PatrolRoute;
            PatrolRouteStartMode resolvedStartMode = defaultStartMode;
            foreach (ProgressionPatrolRule rule in rules)
            {
                if (rule != null && rule.IsMatched(progressionController))
                {
                    resolvedRoute = rule.PatrolRoute;
                    resolvedStartMode = rule.StartMode;
                }
            }

            if (resolvedRoute == null
                || resolvedRoute == lastRoute && resolvedStartMode == lastStartMode)
            {
                return;
            }

            lastRoute = resolvedRoute;
            lastStartMode = resolvedStartMode;
            enemyController.SetPatrolRoute(resolvedRoute, resolvedStartMode);
        }

        public void ReevaluateAfterRestore()
        {
            lastRoute = null;
            EvaluateRules();
        }

        private void OnValidate()
        {
            enemyController ??= GetComponent<EnemyController>();
        }
    }
}
