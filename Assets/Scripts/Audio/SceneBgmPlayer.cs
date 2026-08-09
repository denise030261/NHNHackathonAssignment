using System.Collections;
using NHNHackathon.MainMenu;
using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SceneBgmPlayer : MonoBehaviour
    {
        [Header("BGM")]
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;

        private AudioSource source;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = fadeInDuration > 0f
                ? 0f
                : AudioSettingsController.SavedBgmVolume;
        }

        private void Start()
        {
            if (!playOnStart || clip == null)
            {
                return;
            }

            source.Play();
            if (fadeInDuration > 0f)
            {
                fadeRoutine = StartCoroutine(FadeIn());
            }
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float savedVolume = AudioSettingsController.SavedBgmVolume;
                source.volume = Mathf.Lerp(0f, savedVolume, elapsed / fadeInDuration);
                yield return null;
            }

            source.volume = AudioSettingsController.SavedBgmVolume;
            fadeRoutine = null;
        }

        public void StopImmediately()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            source ??= GetComponent<AudioSource>();
            source.Stop();
        }

        public static void StopAll()
        {
            foreach (SceneBgmPlayer player in FindObjectsByType<SceneBgmPlayer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                player.StopImmediately();
            }
        }

        private void OnDisable()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
        }
    }
}
