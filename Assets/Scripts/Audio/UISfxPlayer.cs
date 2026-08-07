using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class UISfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip hoverClip;
        [SerializeField, Range(0f, 1f)] private float hoverVolumeScale = 1f;

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
            if (source != null && hoverClip != null)
                source.PlayOneShot(hoverClip, hoverVolumeScale);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
