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

        private void Awake()
        {
            Hide();
        }

        public void Show(string label, float elapsed, float duration)
        {
            if (root != null) root.SetActive(true);
            if (stageText != null) stageText.text = label;
            if (progressFill != null)
                progressFill.fillAmount = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
            if (timeText != null) timeText.text = $"{elapsed:0.0} / {duration:0.0}초";
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
