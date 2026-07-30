using UnityEngine;

namespace NHNHackathon.Progression
{
    [CreateAssetMenu(
        fileName = "ProgressionCondition",
        menuName = "NHN Hackathon/Progression/Condition")]
    public sealed class ProgressionCondition : ScriptableObject
    {
        [SerializeField] private string displayName = "Progression Condition";
        [SerializeField, TextArea(2, 4)] private string description;

        public string DisplayName => displayName;
        public string Description => description;
    }
}
