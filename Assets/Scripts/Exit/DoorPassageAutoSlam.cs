using System.Collections;
using System.Collections.Generic;
using NHNHackathon.AudioSystem;
using NHNHackathon.Characters;
using NHNHackathon.MainMenu;
using UnityEngine;
using UnityEngine.Events;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DoorPassageAutoSlam : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExitDoor door;
        [SerializeField, Tooltip("Forward direction determines the two sides of the doorway.")]
        private Transform crossingAxis;

        [Header("Slam")]
        [SerializeField, Min(0.01f), Tooltip("Fast closing time used for the slam.")]
        private float slamDuration = 0.18f;
        [SerializeField, Tooltip("When enabled, the automatic slam occurs only once.")]
        private bool oneShot = true;
        [SerializeField, Min(0f), Tooltip("Minimum travel away from the doorway required when this trigger is placed on one side of the door.")]
        private float minimumOutwardTravel = 0.1f;
        [SerializeField, Min(0f), Tooltip("Wait briefly when the player clears the trigger while the door is still opening.")]
        private float doorOpenWaitDuration = 2f;

        [Header("Slam Audio")]
        [SerializeField, Tooltip("AudioSource used for the slam. When empty, one on this object is found automatically.")]
        private AudioSource slamAudioSource;
        [SerializeField, Tooltip("Door slam clip assigned per passage. The shared SFX library is used when empty.")]
        private AudioClip slamAudioClip;
        [SerializeField, Range(0f, 1f)] private float slamAudioVolume = 1f;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked when the player fully passes through and the door slams.")]
        private UnityEvent onSlammed = new();

        private Transform trackedPlayer;
        private float entrySide;
        private bool hasSlammed;
        private readonly HashSet<int> trackedColliderIds = new();
        private Coroutine pendingSlam;

        public UnityEvent OnSlammed => onSlammed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasSlammed && oneShot)
            {
                return;
            }

            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null)
            {
                return;
            }

            if (trackedPlayer != null && trackedPlayer != player.transform)
            {
                return;
            }

            if (trackedColliderIds.Count == 0)
            {
                trackedPlayer = player.transform;
                entrySide = GetSide(trackedPlayer.position);
            }
            trackedColliderIds.Add(other.GetInstanceID());
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || player.transform != trackedPlayer)
            {
                return;
            }

            trackedColliderIds.Remove(other.GetInstanceID());
            if (trackedColliderIds.Count > 0)
            {
                return;
            }

            float exitSide = GetSide(player.transform.position);
            bool changedSides = Mathf.Abs(entrySide) > Mathf.Epsilon
                && Mathf.Abs(exitSide) > Mathf.Epsilon
                && Mathf.Sign(entrySide) != Mathf.Sign(exitSide);
            float entryDirection = Mathf.Sign(entrySide);
            bool movedOutwardOnSameSide = entryDirection != 0f
                && exitSide * entryDirection
                >= Mathf.Abs(entrySide) + minimumOutwardTravel;
            trackedPlayer = null;

            if ((!changedSides && !movedOutwardOnSameSide) || door == null)
            {
                return;
            }

            if (pendingSlam != null)
            {
                StopCoroutine(pendingSlam);
            }
            pendingSlam = StartCoroutine(SlamWhenDoorIsReady());
        }

        private IEnumerator SlamWhenDoorIsReady()
        {
            float elapsed = 0f;
            while (door != null && !door.IsOpen && elapsed < doorOpenWaitDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            pendingSlam = null;
            if (door != null && door.TrySlamAndSeal(slamDuration))
            {
                hasSlammed = true;
                onSlammed?.Invoke();
                PlaySlamAudio();
            }
        }

        private void PlaySlamAudio()
        {
            if (slamAudioClip != null)
            {
                if (slamAudioSource != null)
                {
                    slamAudioSource.volume = AudioSettingsController.SavedSfxVolume;
                    slamAudioSource.PlayOneShot(slamAudioClip, slamAudioVolume);
                }
                else
                {
                    GameSfxPlayer.PlayAtPoint(
                        slamAudioClip, door.transform.position, slamAudioVolume);
                }
                return;
            }

            GameSfxPlayer.PlayDoorSlam(door.transform.position);
        }

        private float GetSide(Vector3 position)
        {
            Transform axis = crossingAxis != null ? crossingAxis : transform;
            return Vector3.Dot(position - axis.position, axis.forward);
        }

        private void ResolveReferences()
        {
            door ??= GetComponentInParent<ExitDoor>();
            crossingAxis ??= door != null ? door.transform : transform;
            slamAudioSource ??= GetComponent<AudioSource>();
        }

        private void OnDisable()
        {
            trackedColliderIds.Clear();
            trackedPlayer = null;
            pendingSlam = null;
        }

        private void OnValidate()
        {
            ResolveReferences();
            minimumOutwardTravel = Mathf.Max(0f, minimumOutwardTravel);
            doorOpenWaitDuration = Mathf.Max(0f, doorOpenWaitDuration);
            slamAudioVolume = Mathf.Clamp01(slamAudioVolume);
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }
    }
}
