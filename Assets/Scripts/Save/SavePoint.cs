using System.Collections.Generic;
using NHNHackathon.Interaction;
using UnityEngine;

namespace NHNHackathon.SaveSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class SavePoint : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField, Tooltip("Player position and facing direction after restart. Defaults to this object.")]
        private Transform respawnPoint;

        [Header("Feedback")]
        [SerializeField] private bool showSavedMessage = true;
        [SerializeField] private string savedMessage = "저장되었습니다.";
        [SerializeField, Min(0.1f)] private float messageDuration = 1.5f;

        private readonly Dictionary<PlayerCheckpointAgent, int> playerColliderCounts = new();

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerCheckpointAgent player = other.GetComponentInParent<PlayerCheckpointAgent>();
            if (player == null)
            {
                return;
            }

            playerColliderCounts.TryGetValue(player, out int count);
            playerColliderCounts[player] = count + 1;
            if (count > 0)
            {
                return;
            }

            player.SaveCheckpoint(respawnPoint != null ? respawnPoint : transform);
            if (showSavedMessage && !string.IsNullOrWhiteSpace(savedMessage)
                && player.TryGetComponent(out PlayerInteractor interactor))
            {
                interactor.ShowTemporaryMessage(savedMessage, messageDuration);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCheckpointAgent player = other.GetComponentInParent<PlayerCheckpointAgent>();
            if (player == null || !playerColliderCounts.TryGetValue(player, out int count))
            {
                return;
            }

            if (count > 1)
            {
                playerColliderCounts[player] = count - 1;
            }
            else
            {
                playerColliderCounts.Remove(player);
            }
        }

        private void OnDisable()
        {
            playerColliderCounts.Clear();
        }

        private void OnValidate()
        {
            messageDuration = Mathf.Max(0.1f, messageDuration);
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawSphere(
                respawnPoint != null ? respawnPoint.position : transform.position, 0.35f);
        }
    }
}
