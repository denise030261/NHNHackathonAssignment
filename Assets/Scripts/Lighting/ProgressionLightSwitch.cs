using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Lighting
{
    [DisallowMultipleComponent]
    public sealed class ProgressionLightSwitch : MonoBehaviour
    {
        [Header("Condition")]
        [SerializeField] private ProgressionCondition condition;
        [SerializeField] private bool stateWhenCompleted;

        [Header("References")]
        [SerializeField] private Light targetLight;
        [SerializeField] private LightFlickerEffect flickerEffect;

        private GameProgressionController progressionController;

        private void Start()
        {
            TrySubscribe();
            Evaluate();
        }

        private void OnDisable()
        {
            if (progressionController != null)
            {
                progressionController.ProgressionChanged -= Evaluate;
            }
        }

        private void TrySubscribe()
        {
            if (progressionController != null)
            {
                return;
            }

            progressionController = GameProgressionController.Instance;
            if (progressionController != null)
            {
                progressionController.ProgressionChanged += Evaluate;
            }
        }

        private void Evaluate()
        {
            if (progressionController == null || condition == null
                || !progressionController.IsCompleted(condition))
            {
                return;
            }

            if (flickerEffect != null)
            {
                flickerEffect.CancelAndSetState(stateWhenCompleted);
            }
            else if (targetLight != null)
            {
                targetLight.enabled = stateWhenCompleted;
            }
        }

        private void OnValidate()
        {
            if (targetLight == null) targetLight = GetComponent<Light>();
            if (flickerEffect == null) flickerEffect = GetComponent<LightFlickerEffect>();
        }
    }
}
