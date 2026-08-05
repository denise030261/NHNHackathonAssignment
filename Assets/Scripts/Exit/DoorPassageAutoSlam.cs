using NHNHackathon.Characters;
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

        [Header("Events")]
        [SerializeField, Tooltip("Invoked when the player fully passes through and the door slams.")]
        private UnityEvent onSlammed = new();

        private Transform trackedPlayer;
        private float entrySide;
        private bool hasSlammed;

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

            trackedPlayer = player.transform;
            entrySide = GetSide(trackedPlayer.position);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || player.transform != trackedPlayer)
            {
                return;
            }

            float exitSide = GetSide(player.transform.position);
            bool crossedDoorway = entrySide != 0f && exitSide != 0f
                && Mathf.Sign(entrySide) != Mathf.Sign(exitSide);
            trackedPlayer = null;

            if (!crossedDoorway || door == null || !door.IsOpen)
            {
                return;
            }

            if (door.TrySlamAndSeal(slamDuration))
            {
                hasSlammed = true;
                onSlammed?.Invoke();
                // TODO(Audio): SFX 소스가 준비되면 문 쾅 닫힘 효과음을 이 시점에 재생한다.
                // slamAudioSource.PlayOneShot(slamAudioClip);
            }
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
        }

        private void OnValidate()
        {
            ResolveReferences();
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }
    }
}
