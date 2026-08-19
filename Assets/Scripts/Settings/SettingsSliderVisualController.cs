using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Settings
{
    [DisallowMultipleComponent]
    public sealed class SettingsSliderVisualController : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider[] sliders;

        [Header("Handle")]
        [SerializeField, Min(8f)] private float handleSize = 34f;
        [SerializeField] private Color handleColor = Color.white;

        private void Awake()
        {
            Apply();
        }

        [ContextMenu("Apply Slider Handle Style")]
        public void Apply()
        {
            if (sliders == null)
            {
                return;
            }

            foreach (Slider slider in sliders)
            {
                if (slider == null || slider.handleRect == null)
                {
                    continue;
                }

                slider.handleRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, handleSize);
                slider.handleRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, handleSize);
                Image handleImage = slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = handleColor;
                    handleImage.enabled = true;
                }
            }
        }

        private void OnValidate()
        {
            handleSize = Mathf.Max(8f, handleSize);
        }
    }
}
