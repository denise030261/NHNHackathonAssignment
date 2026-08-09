using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class GameSfxPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField, Min(1)] private int initialSize = 12;
        [SerializeField, Min(1)] private int maximumSize = 32;

        [Header("3D Sound")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(0.01f)] private float minimumDistance = 1f;
        [SerializeField, Min(0.01f)] private float maximumDistance = 20f;

        [Header("Occlusion")]
        [SerializeField, Tooltip("Blocks a sound when a solid collider is between it and the listener.")]
        private bool useOcclusion = true;
        [SerializeField, Tooltip("Layers treated as walls or solid sound blockers.")]
        private LayerMask occlusionMask = 1 << 0;
        [SerializeField, Range(0f, 1f), Tooltip("0 fully blocks sounds behind walls; higher values create muffled leakage.")]
        private float occludedVolumeMultiplier;
        [SerializeField, Min(0f), Tooltip("Ignores a hit extremely close to the sound origin, such as its own collider surface.")]
        private float sourceHitIgnoreDistance = 0.08f;

        private static GameSfxPool instance;
        private readonly List<AudioSource> sources = new();
        private readonly List<float> lastPlayedTimes = new();
        private readonly RaycastHit[] occlusionHits = new RaycastHit[16];
        private AudioListener audioListener;

        public static GameSfxPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameSfxPool>(
                        FindObjectsInactive.Include);
                }
                if (instance == null)
                {
                    GameObject root = new("SFX Pool");
                    GameObject audioRoot = GameObject.Find("Audio");
                    if (audioRoot != null)
                    {
                        root.transform.SetParent(audioRoot.transform, false);
                    }
                    instance = root.AddComponent<GameSfxPool>();
                }
                instance.EnsureInitialized();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            EnsureInitialized();
        }

        public void Play(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureInitialized();
            int index = FindAvailableSource();
            AudioSource source = sources[index];
            source.transform.position = position;
            source.clip = clip;
            float occlusionMultiplier = IsOccluded(position)
                ? occludedVolumeMultiplier
                : 1f;
            source.volume = Mathf.Clamp01(volume * occlusionMultiplier);
            source.Play();
            lastPlayedTimes[index] = Time.unscaledTime;
        }

        private void EnsureInitialized()
        {
            maximumSize = Mathf.Max(initialSize, maximumSize);
            sources.RemoveAll(source => source == null);
            while (lastPlayedTimes.Count > sources.Count)
            {
                lastPlayedTimes.RemoveAt(lastPlayedTimes.Count - 1);
            }
            while (sources.Count < initialSize)
            {
                CreateSource();
            }
        }

        private int FindAvailableSource()
        {
            for (int index = 0; index < sources.Count; index++)
            {
                if (!sources[index].isPlaying)
                {
                    return index;
                }
            }

            if (sources.Count < maximumSize)
            {
                CreateSource();
                return sources.Count - 1;
            }

            int oldestIndex = 0;
            float oldestTime = lastPlayedTimes[0];
            for (int index = 1; index < lastPlayedTimes.Count; index++)
            {
                if (lastPlayedTimes[index] >= oldestTime)
                {
                    continue;
                }
                oldestTime = lastPlayedTimes[index];
                oldestIndex = index;
            }
            sources[oldestIndex].Stop();
            return oldestIndex;
        }

        private void CreateSource()
        {
            GameObject sourceObject = new($"Pooled SFX Source {sources.Count + 1:00}");
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minimumDistance;
            source.maxDistance = maximumDistance;
            sources.Add(source);
            lastPlayedTimes.Add(float.NegativeInfinity);
        }

        private bool IsOccluded(Vector3 sourcePosition)
        {
            if (!useOcclusion || occlusionMask.value == 0)
            {
                return false;
            }

            if (audioListener == null || !audioListener.isActiveAndEnabled)
            {
                audioListener = FindAnyObjectByType<AudioListener>();
            }
            if (audioListener == null)
            {
                return false;
            }

            Vector3 offset = audioListener.transform.position - sourcePosition;
            float distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                return false;
            }

            int hitCount = Physics.RaycastNonAlloc(
                sourcePosition, offset / distance, occlusionHits, distance,
                occlusionMask, QueryTriggerInteraction.Ignore);
            Transform listenerRoot = audioListener.transform.root;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = occlusionHits[index];
                if (hit.collider == null || hit.distance <= sourceHitIgnoreDistance)
                {
                    continue;
                }
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == listenerRoot
                    || hitTransform.IsChildOf(listenerRoot))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnValidate()
        {
            initialSize = Mathf.Max(1, initialSize);
            maximumSize = Mathf.Max(initialSize, maximumSize);
            maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
            sourceHitIgnoreDistance = Mathf.Max(0f, sourceHitIgnoreDistance);
        }
    }
}
