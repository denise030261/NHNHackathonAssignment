using UnityEngine;
using UnityEngine.Rendering;

namespace NHNHackathon.Lighting
{
    [DisallowMultipleComponent]
    public sealed class WebHorrorEnvironmentController : MonoBehaviour
    {
        [Header("Ambient Lighting")]
        [SerializeField] private Color ambientColor =
            new Color(0.003f, 0.004f, 0.008f, 1f);
        [SerializeField, Range(0f, 1f)] private float ambientIntensity = 0.04f;

        [Header("Reflections")]
        [SerializeField, Range(0f, 1f)] private float reflectionIntensity = 0.08f;
        [SerializeField, Min(1)] private int reflectionBounces = 1;

        [Header("Camera Background")]
        [SerializeField] private bool forceSolidCameraBackground = true;
        [SerializeField] private Color cameraBackgroundColor = Color.black;

        [Header("Web Performance")]
        [SerializeField, Tooltip("Fog remains disabled by default to avoid unnecessary WebGL cost.")]
        private bool enableFog;

        private void Awake()
        {
            ApplySettings();
        }

        [ContextMenu("Apply Environment Settings")]
        public void ApplySettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.reflectionBounces = Mathf.Max(1, reflectionBounces);
            RenderSettings.fog = enableFog;

            if (!forceSolidCameraBackground)
            {
                return;
            }

            foreach (Camera camera in FindObjectsByType<Camera>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.cameraType != CameraType.Game)
                {
                    continue;
                }
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = cameraBackgroundColor;
            }
        }

        private void OnValidate()
        {
            ambientIntensity = Mathf.Clamp01(ambientIntensity);
            reflectionIntensity = Mathf.Clamp01(reflectionIntensity);
            reflectionBounces = Mathf.Max(1, reflectionBounces);
        }
    }
}
