using System;
using System.Collections;
using System.Collections.Generic;
using NHNHackathon.AI;
using NHNHackathon.AudioSystem;
using NHNHackathon.Enemy;
using UnityEngine;
using UnityEngine.Serialization;

namespace NHNHackathon.Dance
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceSyncZone))]
    [RequireComponent(typeof(DanceSyncJudge))]
    public sealed class DanceZoneDollReactionController : MonoBehaviour
    {
        [Serializable]
        private sealed class FaceBinding
        {
            public Transform Face;
            public Vector3 LocalAimAxis;
        }

        [Header("Dolls")]
        [SerializeField, Tooltip("Root containing this DanceSyncZone and its DancingAI dolls. Defaults to the parent.")]
        private Transform dancerRoot;
        [SerializeField] private bool autoFindFaces = true;
        [FormerlySerializedAs("faceTransformName")]
        [SerializeField, Tooltip("Path from each DancingAI root to the face bone that should track the player.")]
        private string faceTransformPath =
            "CharacterModel/metarig.003/spine/spine.001/spine.002/spine.003/spine.004/face";
        [SerializeField] private List<Transform> additionalFaces = new();

        [Header("Face Tracking")]
        [SerializeField, Tooltip("Uses the direction from face to its first child as the bone's aim axis when possible.")]
        private bool deriveAimAxisFromFirstChild = true;
        [SerializeField, Tooltip("Fallback local aim axis. Blender bones normally point along local Y+.")]
        private Vector3 fallbackLocalAimAxis = Vector3.up;
        [SerializeField, Min(0f), Tooltip("Height above the player's root that the face bones look toward.")]
        private float playerLookHeight = 1.35f;
        [SerializeField, Min(0f)] private float faceTurnSpeed = 12f;

        [Header("Watcher Alert")]
        [SerializeField, Min(0f)] private float watcherAlertRadius = 18f;

        [Header("Detection SFX")]
        [SerializeField]
        private AudioClip screamSfx;
        [SerializeField, Range(0f, 1f)] private float screamVolumeScale = 1f;
        [SerializeField, Min(1)] private int screamRepeatCount = 5;
        [SerializeField, Min(0f), Tooltip("Silence added after each clip before the next repetition.")]
        private float screamRepeatGap = 0.05f;

        private readonly List<FaceBinding> faces = new();
        private DanceSyncZone danceZone;
        private DanceSyncJudge syncJudge;
        private PlayerDanceInput activePlayer;
        private Transform lookTarget;
        private bool playerPerformedDance;
        private bool facesAreTracking;
        private Coroutine screamRoutine;

        private void Awake()
        {
            ResetRuntimeReactionState();
            danceZone = GetComponent<DanceSyncZone>();
            syncJudge = GetComponent<DanceSyncJudge>();
            CacheFaces();
        }

        private void OnEnable()
        {
            ResetRuntimeReactionState();
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
            StopScreamRoutine();
        }

        private void LateUpdate()
        {
            if (!facesAreTracking || lookTarget == null)
            {
                return;
            }

            Vector3 targetPosition = lookTarget.position + Vector3.up * playerLookHeight;
            foreach (FaceBinding binding in faces)
            {
                Transform face = binding.Face;
                if (face == null)
                {
                    continue;
                }

                Vector3 direction = targetPosition - face.position;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                Vector3 currentFaceForward =
                    face.TransformDirection(binding.LocalAimAxis);
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
                    Transform face = dancer.transform.Find(faceTransformPath);
                    if (face != null)
                    {
                        uniqueFaces.Add(face);
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
                    LocalAimAxis = ResolveLocalAimAxis(face)
                });
            }
        }

        private Vector3 ResolveLocalAimAxis(Transform face)
        {
            if (deriveAimAxisFromFirstChild && face.childCount > 0)
            {
                Vector3 childDirection = face.GetChild(0).position - face.position;
                if (childDirection.sqrMagnitude > 0.0001f)
                {
                    return face.InverseTransformDirection(
                        childDirection.normalized).normalized;
                }
            }

            return fallbackLocalAimAxis.sqrMagnitude > 0.0001f
                ? fallbackLocalAimAxis.normalized
                : Vector3.up;
        }

        private void AlertNearestWatcher(Transform player)
        {
            if (player == null)
            {
                return;
            }

            EnemyController nearest = null;
            float nearestDistanceSqr = watcherAlertRadius * watcherAlertRadius;
            foreach (EnemyController watcher in
                     FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
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
            if (screamSfx == null)
            {
                return;
            }

            StopScreamRoutine();
            screamRoutine = StartCoroutine(PlayScreamRepeatedly());
        }

        private IEnumerator PlayScreamRepeatedly()
        {
            int repeatCount = Mathf.Max(1, screamRepeatCount);
            for (int index = 0; index < repeatCount; index++)
            {
                GameSfxPlayer.PlayAtPoint(
                    screamSfx, transform.position, screamVolumeScale);

                if (index < repeatCount - 1)
                {
                    yield return new WaitForSeconds(
                        Mathf.Max(0.01f, screamSfx.length + screamRepeatGap));
                }
            }

            screamRoutine = null;
        }

        private void StopScreamRoutine()
        {
            if (screamRoutine == null)
            {
                return;
            }

            StopCoroutine(screamRoutine);
            screamRoutine = null;
        }

        private void UnsubscribeFromPlayer()
        {
            if (activePlayer != null)
            {
                activePlayer.DanceInputPerformed -= HandlePlayerDanceInput;
            }
            activePlayer = null;
        }

        private void ResetRuntimeReactionState()
        {
            UnsubscribeFromPlayer();
            facesAreTracking = false;
            lookTarget = null;
            playerPerformedDance = false;
        }

        private void OnValidate()
        {
            faceTurnSpeed = Mathf.Max(0f, faceTurnSpeed);
            playerLookHeight = Mathf.Max(0f, playerLookHeight);
            watcherAlertRadius = Mathf.Max(0f, watcherAlertRadius);
            screamRepeatCount = Mathf.Max(1, screamRepeatCount);
            screamRepeatGap = Mathf.Max(0f, screamRepeatGap);
            if (string.IsNullOrWhiteSpace(faceTransformPath))
            {
                faceTransformPath =
                    "CharacterModel/metarig.003/spine/spine.001/spine.002/spine.003/spine.004/face";
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, watcherAlertRadius);
        }
    }
}
