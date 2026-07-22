using NHNHackathon.Dance;
using UnityEngine;

namespace NHNHackathon.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceSequenceController))]
    [RequireComponent(typeof(DanceColorVisualizer))]
    public sealed class AIDanceColorPresenter : MonoBehaviour
    {
        private DanceSequenceController sequenceController;
        private DanceColorVisualizer colorVisualizer;

        private void Awake()
        {
            sequenceController = GetComponent<DanceSequenceController>();
            colorVisualizer = GetComponent<DanceColorVisualizer>();
        }

        private void OnEnable()
        {
            sequenceController ??= GetComponent<DanceSequenceController>();
            sequenceController.DanceStepChanged += HandleDanceStepChanged;
        }

        private void OnDisable()
        {
            if (sequenceController != null)
            {
                sequenceController.DanceStepChanged -= HandleDanceStepChanged;
            }
        }

        private void HandleDanceStepChanged(DanceDefinition dance, int stepIndex, float beatTime)
        {
            colorVisualizer ??= GetComponent<DanceColorVisualizer>();
            colorVisualizer.ShowDance(dance);
        }
    }
}
