using OnlyOnePlayer.Prototype.Characters;
using System;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Mission
{
    public sealed class DataExtractionPoint2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ConfidentialDataMissionController missionController;
        [SerializeField] private Collider2D triggerCollider;

        [Header("Extraction")]
        [SerializeField, Min(0f)] private float requiredStaySeconds = 3f;
        [SerializeField] private bool resetProgressWhenPlayerLeaves = true;

        [Header("Debug")]
        [SerializeField] private bool logProgress;

        private RealPlayerIdentity activePlayer;
        private float stayTimer;
        private bool isCollected;
        private CharacterIdentity collectingActor;

        public bool IsCollected => isCollected;
        public float Progress01 => requiredStaySeconds <= 0f ? 1f : Mathf.Clamp01(stayTimer / requiredStaySeconds);
        public event Action<CharacterIdentity> Collected;

        private void Update()
        {
            if (isCollected || activePlayer == null)
            {
                return;
            }

            stayTimer += Time.deltaTime;
            if (logProgress)
            {
                Debug.Log($"{name} extraction progress: {Progress01:P0}", this);
            }

            if (stayTimer >= requiredStaySeconds)
            {
                Collect();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected || activePlayer != null)
            {
                return;
            }

            RealPlayerIdentity player = other.GetComponent<RealPlayerIdentity>();
            if (player == null || (missionController != null && player != missionController.RealPlayer))
            {
                return;
            }

            activePlayer = player;
            collectingActor = player.CharacterIdentity;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            RealPlayerIdentity player = other.GetComponent<RealPlayerIdentity>();
            if (player == null || player != activePlayer)
            {
                return;
            }

            activePlayer = null;

            if (resetProgressWhenPlayerLeaves && !isCollected)
            {
                stayTimer = 0f;
                collectingActor = null;
            }
        }

        private void Awake()
        {
            if (missionController == null)
            {
                missionController = FindAnyObjectByType<ConfidentialDataMissionController>();
            }

            SetupTrigger();
        }

        private void OnEnable()
        {
            SetupTrigger();
        }

        private void Collect()
        {
            isCollected = true;
            activePlayer = null;
            Collected?.Invoke(collectingActor);
            collectingActor = null;
            missionController?.RegisterDataCollected(this);
        }

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void SetupTrigger()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            triggerCollider.isTrigger = true;
        }
    }
}
