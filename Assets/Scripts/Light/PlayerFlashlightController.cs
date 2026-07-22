using UnityEngine;

namespace NHNHackathon.LightSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LightStimulusSource))]
    public sealed class PlayerFlashlightController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F;
        [SerializeField] private Light flashlight;
        [SerializeField] private bool startEnabled = true;

        private void Start()
        {
            SetFlashlight(startEnabled);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                SetFlashlight(!flashlight.enabled);
            }
        }

        private void SetFlashlight(bool value)
        {
            if (flashlight != null)
            {
                flashlight.enabled = value;
            }
        }

        private void OnValidate()
        {
            if (flashlight == null)
            {
                flashlight = GetComponent<Light>();
            }
        }
    }
}
