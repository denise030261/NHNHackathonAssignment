using System.Collections.Generic;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Lighting
{
    [DisallowMultipleComponent]
    public sealed class ProgressionLightingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameProgressionController progressionController;

        [Header("Rules")]
        [SerializeField, Tooltip("Rules are evaluated top to bottom. Later actions override earlier actions for the same group.")]
        private List<ProgressionLightingRule> rules = new();

        private readonly Dictionary<LightingGroup, LightingAction> resolvedActions = new();
        private readonly Dictionary<LightingGroup, LightingAction> lastAppliedActions = new();
        private readonly HashSet<LightingGroup> previouslyControlledGroups = new();

        private void OnEnable()
        {
            if (progressionController != null)
            {
                progressionController.ProgressionChanged += EvaluateRules;
            }
        }

        private void Start()
        {
            EvaluateRules();
        }

        private void OnDisable()
        {
            if (progressionController != null)
            {
                progressionController.ProgressionChanged -= EvaluateRules;
            }
        }

        [ContextMenu("Evaluate Lighting Rules")]
        public void EvaluateRules()
        {
            if (progressionController == null)
            {
                return;
            }

            resolvedActions.Clear();
            foreach (ProgressionLightingRule rule in rules)
            {
                if (rule == null || !rule.IsMatched(progressionController))
                {
                    continue;
                }

                foreach (LightingAction action in rule.Actions)
                {
                    if (action?.LightingGroup != null)
                    {
                        resolvedActions[action.LightingGroup] = action;
                    }
                }
            }

            foreach (LightingGroup previousGroup in previouslyControlledGroups)
            {
                if (previousGroup != null && !resolvedActions.ContainsKey(previousGroup))
                {
                    previousGroup.RestoreInitialState();
                    lastAppliedActions.Remove(previousGroup);
                }
            }

            previouslyControlledGroups.Clear();
            foreach (KeyValuePair<LightingGroup, LightingAction> resolved in resolvedActions)
            {
                if (!lastAppliedActions.TryGetValue(
                        resolved.Key, out LightingAction previousAction)
                    || previousAction != resolved.Value)
                {
                    resolved.Key.SetState(
                        resolved.Value.TurnOn,
                        resolved.Value.Transition);
                    lastAppliedActions[resolved.Key] = resolved.Value;
                }
                previouslyControlledGroups.Add(resolved.Key);
            }
        }
    }
}
