using System;
using DG.Tweening;
using NHNHackathon.AudioSystem;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DanceSyncZone))]
    [RequireComponent(typeof(DanceSyncJudge))]
    public sealed class TutorialDanceReward : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DanceSyncZone danceZone;
        [SerializeField] private DanceSyncJudge syncJudge;
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private Transform dropPoint;
        [SerializeField, Tooltip("Optional. The player inventory is found automatically when empty.")]
        private PlayerItemInventory playerInventory;

        [Header("Reward Drop SFX")]
        [SerializeField, Tooltip("Played when the tutorial key starts falling.")]
        private AudioClip rewardDropSfx;
        [SerializeField, Range(0f, 1f)] private float rewardDropSfxVolume = 1f;

        [Header("Challenge")]
        [SerializeField, Min(0.01f)] private float requiredSuccessDuration = 3f;
        [SerializeField] private bool resetOnFailedBeat = true;
        [SerializeField] private bool resetOnZoneExit = true;
        [SerializeField] private bool oneShot = true;

        [Header("DOTween Drop Animation")]
        [SerializeField, Min(0f)] private float dropHeight = 2.5f;
        [SerializeField, Min(0.01f)] private float dropDuration = 0.9f;
        [SerializeField] private Ease dropEase = Ease.OutBounce;
        [SerializeField] private Vector3 rotation = new Vector3(0f, 540f, 0f);
        [SerializeField, Min(0.01f)] private float rotationDuration = 0.9f;
        [SerializeField] private Ease rotationEase = Ease.OutCubic;

        private float successfulDuration;
        private bool rewardSpawned;

        public event Action<float> ProgressChanged;
        public event Action<GameObject> RewardDropped;

        public float Progress => Mathf.Clamp01(successfulDuration / requiredSuccessDuration);

        private void Awake()
        {
            danceZone ??= GetComponent<DanceSyncZone>();
            syncJudge ??= GetComponent<DanceSyncJudge>();
            dropPoint ??= transform;
        }

        private void OnEnable()
        {
            syncJudge.DanceStepJudged += HandleDanceStepJudged;
            danceZone.PlayerExited += HandlePlayerExited;
        }

        private void Start()
        {
            SuppressRewardWhenAlreadyCollected();
        }

        private void OnDisable()
        {
            if (syncJudge != null)
            {
                syncJudge.DanceStepJudged -= HandleDanceStepJudged;
            }
            if (danceZone != null)
            {
                danceZone.PlayerExited -= HandlePlayerExited;
            }
        }

        private void HandleDanceStepJudged(DanceStepJudgement judgement)
        {
            SuppressRewardWhenAlreadyCollected();
            if (rewardSpawned && oneShot)
            {
                return;
            }

            if (!judgement.Succeeded)
            {
                if (resetOnFailedBeat)
                {
                    ResetProgress();
                }
                return;
            }

            successfulDuration += Mathf.Max(0f, judgement.BeatDuration);
            ProgressChanged?.Invoke(Progress);
            if (successfulDuration + Mathf.Epsilon >= requiredSuccessDuration)
            {
                DropReward();
            }
        }

        private void HandlePlayerExited(PlayerDanceInput player)
        {
            if (resetOnZoneExit && !(rewardSpawned && oneShot))
            {
                ResetProgress();
            }
        }

        private void ResetProgress()
        {
            if (successfulDuration <= 0f)
            {
                return;
            }
            successfulDuration = 0f;
            ProgressChanged?.Invoke(0f);
        }

        private void DropReward()
        {
            SuppressRewardWhenAlreadyCollected();
            if (rewardPrefab == null || dropPoint == null || (rewardSpawned && oneShot))
            {
                return;
            }

            rewardSpawned = true;
            Vector3 landingPosition = dropPoint.position;
            GameObject reward = Instantiate(
                rewardPrefab, landingPosition + Vector3.up * dropHeight,
                dropPoint.rotation);
            reward.name = rewardPrefab.name;
            GameSfxPlayer.PlayAtPoint(
                rewardDropSfx, reward.transform.position, rewardDropSfxVolume);

            Collider[] colliders = reward.GetComponentsInChildren<Collider>(true);
            KeyCollectible collectible = reward.GetComponentInChildren<KeyCollectible>(true);
            foreach (Collider rewardCollider in colliders)
            {
                rewardCollider.enabled = false;
            }
            if (collectible != null)
            {
                collectible.enabled = false;
            }

            Sequence sequence = DOTween.Sequence().SetLink(reward);
            sequence.Join(reward.transform.DOMove(landingPosition, dropDuration).SetEase(dropEase));
            sequence.Join(reward.transform.DORotate(
                rotation, rotationDuration, RotateMode.FastBeyond360)
                .SetRelative().SetEase(rotationEase));
            sequence.OnComplete(() =>
            {
                foreach (Collider rewardCollider in colliders)
                {
                    if (rewardCollider != null)
                    {
                        rewardCollider.enabled = true;
                    }
                }
                if (collectible != null)
                {
                    collectible.enabled = true;
                }
                // The reward was moved while its colliders were disabled.
                // Synchronize once so proximity queries see the landing position immediately.
                Physics.SyncTransforms();
                RewardDropped?.Invoke(reward);
            });
        }

        private void SuppressRewardWhenAlreadyCollected()
        {
            if (rewardSpawned || rewardPrefab == null)
            {
                return;
            }

            playerInventory ??= FindAnyObjectByType<PlayerItemInventory>();
            KeyCollectible rewardKey =
                rewardPrefab.GetComponentInChildren<KeyCollectible>(true);
            ItemDefinition rewardItem = rewardKey != null
                ? rewardKey.ItemDefinition
                : null;
            bool alreadyOwned = playerInventory != null
                && playerInventory.Contains(rewardItem);
            bool alreadyRecorded = rewardItem != null
                && GameProgressionController.Instance != null
                && GameProgressionController.Instance.IsCompleted(
                    rewardItem.ProgressionCondition);
            if (alreadyOwned || alreadyRecorded)
            {
                rewardSpawned = true;
                successfulDuration = 0f;
            }
        }

        private void OnValidate()
        {
            requiredSuccessDuration = Mathf.Max(0.01f, requiredSuccessDuration);
            dropDuration = Mathf.Max(0.01f, dropDuration);
            rotationDuration = Mathf.Max(0.01f, rotationDuration);
            rewardDropSfxVolume = Mathf.Clamp01(rewardDropSfxVolume);
            danceZone ??= GetComponent<DanceSyncZone>();
            syncJudge ??= GetComponent<DanceSyncJudge>();
        }
    }
}
