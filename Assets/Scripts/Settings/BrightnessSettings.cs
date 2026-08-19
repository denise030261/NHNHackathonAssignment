using System;
using UnityEngine;

namespace NHNHackathon.Settings
{
    public static class BrightnessSettings
    {
        private const string PreferenceKey = "Video.Brightness";
        public const float DefaultValue = 1f;
        public const float MinimumValue = 0.6f;
        public const float MaximumValue = 1.4f;

        public static event Action<float> Changed;

        public static float Value => Mathf.Clamp(
            PlayerPrefs.GetFloat(PreferenceKey, DefaultValue),
            MinimumValue, MaximumValue);

        public static void Set(float value)
        {
            value = Mathf.Clamp(value, MinimumValue, MaximumValue);
            PlayerPrefs.SetFloat(PreferenceKey, value);
            PlayerPrefs.Save();
            Changed?.Invoke(value);
        }

        public static float FromNormalized(float normalized)
        {
            return Mathf.Lerp(MinimumValue, MaximumValue, Mathf.Clamp01(normalized));
        }

        public static float ToNormalized(float value)
        {
            return Mathf.InverseLerp(MinimumValue, MaximumValue, value);
        }
    }
}
