using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class UISfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip hoverClip;
        [SerializeField, Range(0f, 1f)] private float hoverVolumeScale = 1f;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float clickVolumeScale = 1f;

        private AudioSource source;
        public static UISfxPlayer Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public void PlayHover()
        {
            AudioClip clip = hoverClip != null
                ? hoverClip
                : GameSfxPlayer.Library?.UiHovered;
            if (source != null && clip != null)
                source.PlayOneShot(clip, hoverVolumeScale);
        }

        public void PlayClick()
        {
            AudioClip clip = clickClip != null
                ? clickClip
                : GameSfxPlayer.Library?.UiClick;
            if (source != null && clip != null)
                source.PlayOneShot(clip, clickVolumeScale);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
