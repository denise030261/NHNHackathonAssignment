using System.Collections;
using System;
using System.IO;
using NHNHackathon.AudioSystem;
using NHNHackathon.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class EndingCreditsController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject endingRoot;
        [SerializeField] private RectTransform creditsViewport;
        [SerializeField] private RectTransform creditsContent;
        [SerializeField] private Image fadeImage;

        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private AudioSource videoAudioSource;
        [SerializeField, Tooltip("File copied under Assets/StreamingAssets for WebGL URL playback.")]
        private string streamingVideoFileName = "Ending.mp4";
        [SerializeField, Min(0.1f)] private float prepareTimeout = 10f;

        [Header("Credits")]
        [SerializeField, Min(1f)] private float scrollSpeed = 70f;
        [SerializeField, Min(1f)] private float holdSpaceSpeedMultiplier = 4f;
        [SerializeField, Min(0f)] private float startDelay = 0.75f;
        [SerializeField, Min(0f)] private float screenPadding = 80f;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float fadeInDuration = 1f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 1f;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private Coroutine endingRoutine;
        private bool videoPrepareFailed;
        private bool isLoadingScene;

        public bool IsPlaying => endingRoutine != null;

        private void Awake()
        {
            if (endingRoot != null)
            {
                endingRoot.SetActive(false);
            }

            ConfigureVideoPlayer();
        }

        public void PlayEnding()
        {
            if (endingRoutine != null || isLoadingScene)
            {
                return;
            }

            SceneBgmPlayer.StopAll();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;

            if (endingRoot != null)
            {
                endingRoot.SetActive(true);
            }

            endingRoutine = StartCoroutine(PlayEndingRoutine());
        }

        private IEnumerator PlayEndingRoutine()
        {
            SetFadeAlpha(1f);
            yield return PrepareAndStartVideo();
            yield return null;

            Canvas.ForceUpdateCanvases();
            float viewportHeight = creditsViewport != null
                ? creditsViewport.rect.height
                : Screen.height;
            float contentHeight = creditsContent != null
                ? creditsContent.rect.height
                : viewportHeight;
            float startY = -(viewportHeight + contentHeight) * 0.5f - screenPadding;
            float endY = (viewportHeight + contentHeight) * 0.5f + screenPadding;
            SetCreditsPosition(startY);

            yield return Fade(1f, 0f, fadeInDuration);
            yield return WaitUnscaled(startDelay);

            float currentY = startY;
            while (currentY < endY)
            {
                float multiplier = UnityEngine.Input.GetKey(KeyCode.Space)
                    ? holdSpaceSpeedMultiplier
                    : 1f;
                currentY += scrollSpeed * multiplier * Time.unscaledDeltaTime;
                SetCreditsPosition(Mathf.Min(currentY, endY));
                yield return null;
            }

            yield return Fade(0f, 1f, fadeOutDuration);
            videoPlayer?.Stop();
            yield return LoadMainMenu();
        }

        private IEnumerator PrepareAndStartVideo()
        {
            if (videoPlayer == null || string.IsNullOrWhiteSpace(videoPlayer.url))
            {
                Debug.LogError("Ending video is not assigned.", this);
                yield break;
            }

            videoPrepareFailed = false;
            videoPlayer.errorReceived += HandleVideoError;
            videoPlayer.Prepare();

            float elapsed = 0f;
            while (!videoPlayer.isPrepared
                   && !videoPrepareFailed
                   && elapsed < prepareTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            videoPlayer.errorReceived -= HandleVideoError;
            if (!videoPlayer.isPrepared)
            {
                Debug.LogWarning(
                    "Ending video preparation timed out. Credits will continue without waiting.", this);
                yield break;
            }

            if (videoAudioSource != null)
            {
                videoAudioSource.volume = AudioSettingsController.SavedBgmVolume;
            }
            videoPlayer.Play();
        }

        private void ConfigureVideoPlayer()
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = BuildStreamingVideoUrl();
            videoPlayer.isLooping = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        }

        private string BuildStreamingVideoUrl()
        {
            string fileName = string.IsNullOrWhiteSpace(streamingVideoFileName)
                ? "Ending.mp4"
                : streamingVideoFileName;
#if UNITY_WEBGL && !UNITY_EDITOR
            return $"{Application.streamingAssetsPath.TrimEnd('/')}/{Uri.EscapeDataString(fileName)}";
#else
            string localPath = Path.Combine(Application.streamingAssetsPath, fileName);
            return new Uri(localPath).AbsoluteUri;
#endif
        }

        private void HandleVideoError(VideoPlayer source, string message)
        {
            videoPrepareFailed = true;
            Debug.LogError($"Ending video error: {message}", source);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (fadeImage == null || duration <= 0f)
            {
                SetFadeAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetFadeAlpha(to);
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator LoadMainMenu()
        {
            if (isLoadingScene || string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                yield break;
            }

            isLoadingScene = true;
            endingRoutine = null;
            Time.timeScale = 1f;
            AsyncOperation operation = SceneManager.LoadSceneAsync(mainMenuSceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        private void SetCreditsPosition(float y)
        {
            if (creditsContent == null)
            {
                return;
            }

            Vector2 position = creditsContent.anchoredPosition;
            position.y = y;
            creditsContent.anchoredPosition = position;
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeImage == null)
            {
                return;
            }

            Color color = fadeImage.color;
            color.a = Mathf.Clamp01(alpha);
            fadeImage.color = color;
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.errorReceived -= HandleVideoError;
            }
        }

        private void OnDestroy()
        {
            if (!isLoadingScene && Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
