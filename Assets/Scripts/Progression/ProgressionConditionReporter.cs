using UnityEngine;

namespace NHNHackathon.Progression
{
    [DisallowMultipleComponent]
    public sealed class ProgressionConditionReporter : MonoBehaviour
    {
        [SerializeField] private ProgressionCondition condition;
        [SerializeField, Tooltip("Complete this condition automatically when enabled.")]
        private bool completeOnEnable;

        private void OnEnable()
        {
            if (completeOnEnable)
            {
                CompleteCondition();
            }
        }

        public void CompleteCondition()
        {
            GameProgressionController.Instance?.TryComplete(condition);
        }
    }
}
