using System;
using System.Collections.Generic;
using NHNHackathon.AI;
using NHNHackathon.AudioSystem;
using NHNHackathon.Enemy;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceSyncZone))]
    [RequireComponent(typeof(DanceSyncJudge))]
    public sealed class DanceZoneDollReactionController : MonoBehaviour
    {
        [Serializable]
        private sealed class FaceBinding
        {
            public Transform Face;
            public Quaternion OriginalLocalRotation;
        }

        [Header("Dolls")]
        [SerializeField, Tooltip("Root containing this DanceSyncZone and its DancingAI dolls. Defaults to the parent.")]
        private Transform dancerRoot;
        [SerializeField] private bool autoFindFaces = true;
        [SerializeField, Tooltip("Exact transform name used by the character model.")]
        private string faceTransformName = "face";
        [SerializeField] private List<Transform> additionalFaces = new();

        [Header("Face Tracking")]
        [SerializeField] private Vector3 localFaceForwardAxis = Vector3.forward;
        [SerializeField, Min(0f)] private float faceTurnSpeed = 12f;

        [Header("Watcher Alert")]
        [SerializeField, Min(0f)] private float watcherAlertRadius = 18f;

        [Header("Scream SFX (Optional)")]
        [SerializeField, Tooltip("Assign the doll scream clip here when the resource is ready.")]
        private AudioClip screamSfx;
        [SerializeField, Range(0f, 1f)] private float screamVolumeScale = 1f;

        private readonly List<FaceBinding> faces = new();
        private DanceSyncZone danceZone;
        private DanceSyncJudge syncJudge;
        private PlayerDanceInput activePlayer;
        private Transform lookTarget;
        private bool playerPerformedDance;
        private bool facesAreTracking;

        private void Awake()
        {
            danceZone = GetComponent<DanceSyncZone>();
            syncJudge = GetComponent<DanceSyncJudge>();
            CacheFaces();
        }

        private void OnEnable()
        {
            danceZone ??= GetComponent<DanceSyncZone>();
            syncJudge ??= GetComponent<DanceSyncJudge>();
            danceZone.PlayerEntered += HandlePlayerEntered;
            danceZone.PlayerExited += HandlePlayerExited;
            syncJudge.DanceStepJudged += HandleDanceStepJudged;
        }

        private void OnDisable()
        {
            if (danceZone != null)
            {
                danceZone.PlayerEntered -= HandlePlayerEntered;
                danceZone.PlayerExited -= HandlePlayerExited;
            }
            if (syncJudge != null)
            {
                syncJudge.DanceStepJudged -= HandleDanceStepJudged;
            }
            UnsubscribeFromPlayer();
        }

        private void LateUpdate()
        {
            if (!facesAreTracking || lookTarget == null)
            {
                return;
            }

            Vector3 forwardAxis = localFaceForwardAxis.sqrMagnitude > 0.0001f
                ? localFaceForwardAxis.normalized
                : Vector3.forward;
            foreach (FaceBinding binding in faces)
            {
                Transform face = binding.Face;
                if (face == null)
                {
                    continue;
                }

                Vector3 direction = lookTarget.position - face.position;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                Vector3 currentFaceForward = face.TransformDirection(forwardAxis);
                Quaternion targetRotation =
                    Quaternion.FromToRotation(currentFaceForward, direction.normalized)
                    * face.rotation;
                face.rotation = Quaternion.Slerp(
                    face.rotation, targetRotation, faceTurnSpeed * Time.deltaTime);
            }
        }

        private void HandlePlayerEntered(PlayerDanceInput player)
        {
            UnsubscribeFromPlayer();
            activePlayer = player;
            lookTarget = player != null ? player.transform : lookTarget;
            playerPerformedDance = false;
            if (activePlayer != null)
            {
                activePlayer.DanceInputPerformed += HandlePlayerDanceInput;
            }
        }

        private void HandlePlayerExited(PlayerDanceInput player)
        {
            bool passedWithoutDancing = player == activePlayer && !playerPerformedDance;
            lookTarget = player != null ? player.transform : lookTarget;
            UnsubscribeFromPlayer();

            if (!passedWithoutDancing)
            {
                return;
            }

            SetFaceTracking(true);
            PlayScream();
            AlertNearestWatcher(player != null ? player.transform : null);
        }

        private void HandlePlayerDanceInput(int danceId, float inputTime)
        {
            playerPerformedDance = true;
        }

        private void HandleDanceStepJudged(DanceStepJudgement judgement)
        {
            if (judgement.Succeeded)
            {
                SetFaceTracking(false);
                return;
            }

            // -1 means no button was pressed for the beat. That case is handled on zone exit.
            if (activePlayer != null && judgement.PlayerDanceId > 0)
            {
                lookTarget = activePlayer.transform;
                SetFaceTracking(true);
            }
        }

        private void SetFaceTracking(bool enabled)
        {
            facesAreTracking = enabled;
            if (enabled)
            {
                return;
            }

            foreach (FaceBinding binding in faces)
            {
                if (binding.Face != null)
                {
                    binding.Face.localRotation = binding.OriginalLocalRotation;
                }
            }
        }

        private void CacheFaces()
        {
            faces.Clear();
            HashSet<Transform> uniqueFaces = new();
            dancerRoot ??= transform.parent != null ? transform.parent : transform;

            if (autoFindFaces && dancerRoot != null)
            {
                foreach (DanceSequenceController dancer in
                         dancerRoot.GetComponentsInChildren<DanceSequenceController>(true))
                {
                    foreach (Transform child in dancer.GetComponentsInChildren<Transform>(true))
                    {
                        if (string.Equals(child.name, faceTransformName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            uniqueFaces.Add(child);
                        }
                    }
                }
            }

            foreach (Transform face in additionalFaces)
            {
                if (face != null)
                {
                    uniqueFaces.Add(face);
                }
            }

            foreach (Transform face in uniqueFaces)
            {
                faces.Add(new FaceBinding
                {
                    Face = face,
                    OriginalLocalRotation = face.localRotation
                });
            }
        }

        private void AlertNearestWatcher(Transform player)
        {
            if (player == null)
            {
                return;
            }

            EnemyController nearest = null;
            float nearestDistanceSqr = watcherAlertRadius * watcherAlertRadius;
            foreach (EnemyController watcher in FindObjectsByType<EnemyController>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!watcher.isActiveAndEnabled)
                {
                    continue;
                }

                float distanceSqr = (watcher.transform.position - player.position).sqrMagnitude;
                if (distanceSqr <= nearestDistanceSqr)
                {
                    nearest = watcher;
                    nearestDistanceSqr = distanceSqr;
                }
            }

            nearest?.AlertToPlayer(player);
        }

        private void PlayScream()
        {
            // SFX resource is not available yet. Assign Scream Sfx in the Inspector later.
            if (screamSfx != null)
            {
                GameSfxPlayer.PlayAtPoint(screamSfx, transform.position, screamVolumeScale);
            }
        }

        private void UnsubscribeFromPlayer()
        {
            if (activePlayer != null)
            {
                activePlayer.DanceInputPerformed -= HandlePlayerDanceInput;
            }
            activePlayer = null;
        }

        private void OnValidate()
        {
            faceTurnSpeed = Mathf.Max(0f, faceTurnSpeed);
            watcherAlertRadius = Mathf.Max(0f, watcherAlertRadius);
            if (string.IsNullOrWhiteSpace(faceTransformName))
            {
                faceTransformName = "face";
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, watcherAlertRadius);
        }
    }
}
