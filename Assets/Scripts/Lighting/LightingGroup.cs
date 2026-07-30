using System.Collections;
using UnityEngine;

namespace NHNHackathon.Lighting
{
    [DisallowMultipleComponent]
    public sealed class LightingGroup : MonoBehaviour
    {
        [Header("Controlled Objects")]
        [SerializeField] private Light[] lights;
        [SerializeField, Tooltip("Optional emissive meshes or helper objects toggled with the lights.")]
        private GameObject[] linkedObjects;

        private bool initialState;
        private Coroutine transitionRoutine;

        public bool CurrentState { get; private set; }

        private void Awake()
        {
            initialState = GetCurrentSceneState();
            CurrentState = initialState;
        }

        public void SetState(bool turnOn, LightTransitionSettings transition)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }
            transitionRoutine = StartCoroutine(ApplyState(turnOn, transition));
        }

        public void RestoreInitialState()
        {
            SetState(initialState, LightTransitionSettings.Immediate);
        }

        private IEnumerator ApplyState(bool turnOn, LightTransitionSettings transition)
        {
            if (transition.Delay > 0f)
            {
                yield return new WaitForSeconds(transition.Delay);
            }

            if (transition.UseFlicker)
            {
                for (int index = 0; index < transition.FlickerCount; index++)
                {
                    ApplyImmediate(!turnOn);
                    yield return new WaitForSeconds(transition.FlickerInterval);
                    ApplyImmediate(turnOn);
                    yield return new WaitForSeconds(transition.FlickerInterval);
                }
            }

            ApplyImmediate(turnOn);
            transitionRoutine = null;
        }

        private void ApplyImmediate(bool value)
        {
            foreach (Light targetLight in lights)
            {
                if (targetLight != null)
                {
                    targetLight.enabled = value;
                }
            }

            foreach (GameObject linkedObject in linkedObjects)
            {
                if (linkedObject != null)
                {
                    linkedObject.SetActive(value);
                }
            }
            CurrentState = value;
        }

        private bool GetCurrentSceneState()
        {
            foreach (Light targetLight in lights)
            {
                if (targetLight != null && targetLight.enabled)
                {
                    return true;
                }
            }

            foreach (GameObject linkedObject in linkedObjects)
            {
                if (linkedObject != null && linkedObject.activeSelf)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
