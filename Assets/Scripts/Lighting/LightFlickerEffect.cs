using System.Collections;
using NHNHackathon.AudioSystem;
using UnityEngine;

namespace NHNHackathon.Lighting
{
    [DisallowMultipleComponent]
    public sealed class LightFlickerEffect : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Light targetLight;

        [Header("Flicker")]
        [SerializeField, Min(1)] private int flickerCount = 5;
        [SerializeField, Min(0.01f)] private float minimumInterval = 0.06f;
        [SerializeField, Min(0.01f)] private float maximumInterval = 0.16f;

        private Coroutine flickerRoutine;

        public void Play()
        {
            if (targetLight == null)
            {
                return;
            }

            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
            }
            GameSfxPlayer.PlayLightFlicker(transform.position);
            flickerRoutine = StartCoroutine(Flicker());
        }

        public void CancelAndSetState(bool enabled)
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }
            if (targetLight != null)
            {
                targetLight.enabled = enabled;
            }
        }

        private IEnumerator Flicker()
        {
            bool originalState = targetLight.enabled;
            for (int index = 0; index < flickerCount; index++)
            {
                targetLight.enabled = !originalState;
                yield return new WaitForSeconds(Random.Range(minimumInterval, maximumInterval));
                targetLight.enabled = originalState;
                yield return new WaitForSeconds(Random.Range(minimumInterval, maximumInterval));
            }

            targetLight.enabled = originalState;
            flickerRoutine = null;
        }

        private void OnDisable()
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }
        }

        private void OnValidate()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }
            maximumInterval = Mathf.Max(minimumInterval, maximumInterval);
        }
    }
}
