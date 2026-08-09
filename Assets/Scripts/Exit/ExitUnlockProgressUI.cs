using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class ExitUnlockProgressUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text stageText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text timeText;

        [Header("Fill Behaviour")]
        [SerializeField, Tooltip("Resize the fill RectTransform horizontally. This also works without a Source Image sprite.")]
        private bool useRectTransformFill = true;

        private void Awake()
        {
            Hide();
        }

        public void Show(string label, float elapsed, float duration)
        {
            float normalizedProgress = duration > 0f
                ? Mathf.Clamp01(elapsed / duration)
                : 0f;
            if (root != null) root.SetActive(true);
            if (stageText != null) stageText.text = label;
            if (progressFill != null)
            {
                ApplyFill(normalizedProgress);
            }
            if (timeText != null)
            {
                int percentage = Mathf.RoundToInt(normalizedProgress * 100f);
                timeText.text = $"{percentage}%  ({elapsed:0.0} / {duration:0.0}초)";
            }
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void ApplyFill(float normalizedProgress)
        {
            progressFill.fillAmount = normalizedProgress;
            if (!useRectTransformFill)
            {
                progressFill.type = Image.Type.Filled;
                progressFill.fillMethod = Image.FillMethod.Horizontal;
                progressFill.fillOrigin = 0;
                return;
            }

            // Image.Type.Filled ignores fillAmount when no Source Image is assigned.
            // Resizing the child rect keeps the bar functional with a plain color Image.
            progressFill.type = Image.Type.Simple;
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalizedProgress, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }
    }
}
