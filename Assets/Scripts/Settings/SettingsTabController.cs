using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Settings
{
    [DisallowMultipleComponent]
    public sealed class SettingsTabController : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button musicButton;
        [SerializeField] private Button lightButton;

        [Header("Tab Contents")]
        [SerializeField] private GameObject musicContent;
        [SerializeField] private GameObject lightContent;

        [Header("Button Colors")]
        [SerializeField] private Color selectedColor =
            new(0.72f, 0.64f, 0.48f, 1f);
        [SerializeField] private Color normalColor =
            new(0.18f, 0.18f, 0.21f, 1f);

        private void OnEnable()
        {
            if (musicButton != null)
            {
                musicButton.onClick.AddListener(ShowMusic);
            }
            if (lightButton != null)
            {
                lightButton.onClick.AddListener(ShowLight);
            }

            ShowMusic();
        }

        private void OnDisable()
        {
            if (musicButton != null)
            {
                musicButton.onClick.RemoveListener(ShowMusic);
            }
            if (lightButton != null)
            {
                lightButton.onClick.RemoveListener(ShowLight);
            }
        }

        public void ShowMusic()
        {
            SetTab(true);
        }

        public void ShowLight()
        {
            SetTab(false);
        }

        private void SetTab(bool showMusic)
        {
            if (musicContent != null)
            {
                musicContent.SetActive(showMusic);
            }
            if (lightContent != null)
            {
                lightContent.SetActive(!showMusic);
            }

            SetButtonColor(musicButton, showMusic ? selectedColor : normalColor);
            SetButtonColor(lightButton, showMusic ? normalColor : selectedColor);
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = color;
            }
        }
    }
}
