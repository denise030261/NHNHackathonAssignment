using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.Settings
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ScreenBrightnessEffect : MonoBehaviour
    {
        private const string ShaderResourceName = "ScreenBrightness";
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        private Material material;
        private float brightness = BrightnessSettings.DefaultValue;

        private void Awake()
        {
            BrightnessSettings.Changed += ApplyBrightness;
            ApplyBrightness(BrightnessSettings.Value);
        }

        private void ApplyBrightness(float value)
        {
            brightness = Mathf.Clamp(
                value, BrightnessSettings.MinimumValue,
                BrightnessSettings.MaximumValue);
            enabled = !Mathf.Approximately(
                brightness, BrightnessSettings.DefaultValue);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            EnsureMaterial();
            if (material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetFloat(BrightnessId, brightness);
            Graphics.Blit(source, destination, material);
        }

        private void EnsureMaterial()
        {
            if (material != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>(ShaderResourceName);
            if (shader != null)
            {
                material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        private void OnDestroy()
        {
            BrightnessSettings.Changed -= ApplyBrightness;
            if (material != null)
            {
                Destroy(material);
            }
        }
    }

    public static class ScreenBrightnessBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Camera camera = Camera.main;
            if (camera != null && camera.GetComponent<ScreenBrightnessEffect>() == null)
            {
                camera.gameObject.AddComponent<ScreenBrightnessEffect>();
            }
        }
    }
}
