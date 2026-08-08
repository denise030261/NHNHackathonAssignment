using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NHNHackathon.SaveSystem;

namespace NHNHackathon.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string gameplaySceneName = "Level1";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject webQuitMessage;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("WebGL Feedback")]
        [SerializeField, Min(0f)] private float webQuitMessageDuration = 2.5f;

        private bool isLoading;
        private Coroutine quitMessageRoutine;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            if (webQuitMessage != null)
            {
                webQuitMessage.SetActive(false);
            }
        }

        public void StartGame()
        {
            if (isLoading || string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return;
            }

            isLoading = true;
            CheckpointSession.ResetForNewGame();
            SetMainButtonsInteractable(false);
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(gameplaySceneName);
        }

        public void OpenSettings()
        {
            if (!isLoading && settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            if (quitMessageRoutine != null)
            {
                StopCoroutine(quitMessageRoutine);
            }
            quitMessageRoutine = StartCoroutine(ShowWebQuitMessage());
            Application.Quit();
#else
            Application.Quit();
#endif
        }

        private IEnumerator ShowWebQuitMessage()
        {
            if (webQuitMessage == null)
            {
                yield break;
            }

            webQuitMessage.SetActive(true);
            yield return new WaitForSecondsRealtime(webQuitMessageDuration);
            webQuitMessage.SetActive(false);
            quitMessageRoutine = null;
        }

        private void SetMainButtonsInteractable(bool value)
        {
            if (startButton != null)
            {
                startButton.interactable = value;
            }
            if (settingsButton != null)
            {
                settingsButton.interactable = value;
            }
            if (quitButton != null)
            {
                quitButton.interactable = value;
            }
        }
    }
}
