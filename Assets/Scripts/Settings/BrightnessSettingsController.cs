using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Settings
{
    [DisallowMultipleComponent]
    public sealed class BrightnessSettingsController : MonoBehaviour
    {
        private const string PreviewShaderResourceName = "BrightnessPreview";
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        [Header("UI")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Text brightnessValueText;

        [Header("Brightness Preview")]
        [SerializeField] private RawImage recommendedPreviewImage;
        [SerializeField] private RawImage currentPreviewImage;
        [SerializeField] private Texture recommendedPreviewTexture;
        [SerializeField] private Texture currentPreviewTexture;

        private Material currentPreviewMaterial;

        private void Awake()
        {
            ApplyPreviewTextures();

            if (brightnessSlider == null)
            {
                return;
            }

            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.wholeNumbers = false;
            float normalized = BrightnessSettings.ToNormalized(
                BrightnessSettings.Value);
            brightnessSlider.SetValueWithoutNotify(normalized);
            UpdateValueText(normalized);
            UpdatePreview(BrightnessSettings.Value);
        }

        private void OnEnable()
        {
            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(SetBrightness);
            }
        }

        private void OnDisable()
        {
            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
            }
        }

        public void SetBrightness(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            float brightness = BrightnessSettings.FromNormalized(normalized);
            BrightnessSettings.Set(brightness);
            UpdateValueText(normalized);
            UpdatePreview(brightness);
        }

        [ContextMenu("Apply Preview Images")]
        public void ApplyPreviewTextures()
        {
            ApplyPreviewTexture(
                recommendedPreviewImage, recommendedPreviewTexture);
            ApplyPreviewTexture(currentPreviewImage, currentPreviewTexture);
        }

        private static void ApplyPreviewTexture(RawImage image, Texture texture)
        {
            if (image == null)
            {
                return;
            }

            image.texture = texture;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyPreviewTextures();
        }
#endif

        private void UpdatePreview(float brightness)
        {
            if (currentPreviewImage == null)
            {
                return;
            }

            EnsurePreviewMaterial();
            if (currentPreviewMaterial != null)
            {
                currentPreviewMaterial.SetFloat(BrightnessId, brightness);
            }
        }

        private void EnsurePreviewMaterial()
        {
            if (currentPreviewMaterial != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>(PreviewShaderResourceName);
            if (shader == null)
            {
                return;
            }

            currentPreviewMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            currentPreviewImage.material = currentPreviewMaterial;
        }

        private void UpdateValueText(float normalized)
        {
            if (brightnessValueText != null)
            {
                brightnessValueText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
            }
        }

        private void OnDestroy()
        {
            if (currentPreviewMaterial != null)
            {
                Destroy(currentPreviewMaterial);
            }
        }
    }
}
