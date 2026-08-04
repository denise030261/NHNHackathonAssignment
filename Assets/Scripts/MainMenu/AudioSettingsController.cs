using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace NHNHackathon.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class AudioSettingsController : MonoBehaviour
    {
        private const string BgmPreferenceKey = "Audio.BGMVolume";
        private const string SfxPreferenceKey = "Audio.SFXVolume";

        [Header("Optional Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmVolumeParameter = "BGMVolume";
        [SerializeField] private string sfxVolumeParameter = "SFXVolume";

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private List<AudioSource> sfxSources = new();

        [Header("UI")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Text bgmValueText;
        [SerializeField] private Text sfxValueText;

        public static float SavedBgmVolume => PlayerPrefs.GetFloat(BgmPreferenceKey, 1f);
        public static float SavedSfxVolume => PlayerPrefs.GetFloat(SfxPreferenceKey, 1f);

        private void Start()
        {
            float bgmVolume = SavedBgmVolume;
            float sfxVolume = SavedSfxVolume;
            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(bgmVolume);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(sfxVolume);
            }
            ApplyBgmVolume(bgmVolume, false);
            ApplySfxVolume(sfxVolume, false);
        }

        public void SetBgmVolume(float value)
        {
            ApplyBgmVolume(value, true);
        }

        public void SetSfxVolume(float value)
        {
            ApplySfxVolume(value, true);
        }

        public void PlayMenuSfx(AudioClip clip)
        {
            AudioSource source = sfxSources.Count > 0 ? sfxSources[0] : null;
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }
        }

        private void ApplyBgmVolume(float value, bool save)
        {
            value = Mathf.Clamp01(value);
            bool mixerApplied = TrySetMixerVolume(bgmVolumeParameter, value);
            if (!mixerApplied && bgmSource != null)
            {
                bgmSource.volume = value;
            }
            SetPercentage(bgmValueText, value);
            if (save)
            {
                PlayerPrefs.SetFloat(BgmPreferenceKey, value);
                PlayerPrefs.Save();
            }
        }

        private void ApplySfxVolume(float value, bool save)
        {
            value = Mathf.Clamp01(value);
            bool mixerApplied = TrySetMixerVolume(sfxVolumeParameter, value);
            if (!mixerApplied)
            {
                foreach (AudioSource source in sfxSources)
                {
                    if (source != null)
                    {
                        source.volume = value;
                    }
                }
            }
            SetPercentage(sfxValueText, value);
            if (save)
            {
                PlayerPrefs.SetFloat(SfxPreferenceKey, value);
                PlayerPrefs.Save();
            }
        }

        private bool TrySetMixerVolume(string parameter, float value)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameter))
            {
                return false;
            }

            float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
            return audioMixer.SetFloat(parameter, decibels);
        }

        private static void SetPercentage(Text target, float value)
        {
            if (target != null)
            {
                target.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }
    }
}
