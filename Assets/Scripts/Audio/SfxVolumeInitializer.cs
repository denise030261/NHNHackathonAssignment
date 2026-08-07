using NHNHackathon.MainMenu;
using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxVolumeInitializer : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<AudioSource>().volume = AudioSettingsController.SavedSfxVolume;
        }
    }
}
