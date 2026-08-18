using System.Collections;
using NHNHackathon.Dance;
using NHNHackathon.Enemy;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.SaveSystem
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerItemInventory))]
    public sealed class PlayerCheckpointAgent : MonoBehaviour
    {
        private PlayerItemInventory inventory;
        private PlayerDanceUnlockController danceUnlockController;

        private void Awake()
        {
            inventory = GetComponent<PlayerItemInventory>();
            danceUnlockController = GetComponent<PlayerDanceUnlockController>();
        }

        private void Start()
        {
            if (CheckpointSession.TryConsumeRespawn(out CheckpointSnapshot snapshot))
            {
                Restore(snapshot);
                return;
            }

            CheckpointSession.RegisterInitialSpawn(this);
        }

        public bool SaveCheckpoint(int saveIndex, Transform respawnPoint)
        {
            Transform point = respawnPoint != null ? respawnPoint : transform;
            return CheckpointSession.SaveCheckpoint(
                this, saveIndex, point.position, point.rotation);
        }

        public bool HasSameItems(
            System.Collections.Generic.IReadOnlyList<ItemDefinition> items)
        {
            if (items == null || inventory.Items.Count != items.Count)
            {
                return false;
            }

            foreach (ItemDefinition item in items)
            {
                if (!inventory.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        public void PrepareRespawn()
        {
            CheckpointSession.PrepareRespawn(this);
        }

        public void WriteProgressTo(CheckpointSnapshot snapshot)
        {
            snapshot.Items.Clear();
            snapshot.Items.AddRange(inventory.Items);

            snapshot.Conditions.Clear();
            GameProgressionController progression = GameProgressionController.Instance;
            if (progression != null)
            {
                snapshot.Conditions.AddRange(progression.CompletedConditions);
            }
            foreach (ItemDefinition item in inventory.Items)
            {
                ProgressionCondition itemCondition = item != null ? item.ProgressionCondition : null;
                if (itemCondition != null && !snapshot.Conditions.Contains(itemCondition))
                {
                    snapshot.Conditions.Add(itemCondition);
                }
            }

            snapshot.UnlockedDanceIds.Clear();
            if (danceUnlockController != null)
            {
                snapshot.UnlockedDanceIds.AddRange(danceUnlockController.UnlockedDanceIds);
            }
        }

        private void Restore(CheckpointSnapshot snapshot)
        {
            CharacterController characterController = GetComponent<CharacterController>();
            bool wasEnabled = characterController != null && characterController.enabled;
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);

            if (characterController != null)
            {
                characterController.enabled = wasEnabled;
            }

            inventory.Restore(snapshot.Items);
            GameProgressionController.Instance?.Restore(snapshot.Conditions);
            danceUnlockController?.Restore(snapshot.UnlockedDanceIds);
            HideAlreadyCollectedWorldItems();
            StartCoroutine(ReapplyProgressionAfterSceneStart(snapshot));
        }

        private static IEnumerator ReapplyProgressionAfterSceneStart(
            CheckpointSnapshot snapshot)
        {
            // Progression listeners subscribe in Start. Wait until every Start has completed,
            // then publish the restored state again and force route selection to be reapplied.
            yield return null;

            GameProgressionController.Instance?.Restore(snapshot.Conditions);
            foreach (ProgressionPatrolController patrolController in
                     FindObjectsByType<ProgressionPatrolController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                patrolController.ReevaluateAfterRestore();
            }
        }

        private void HideAlreadyCollectedWorldItems()
        {
            foreach (InspectableItem item in FindObjectsByType<InspectableItem>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (inventory.Contains(item.Item))
                {
                    item.ApplyCollectedState();
                }
            }

            foreach (KeyCollectible key in FindObjectsByType<KeyCollectible>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (inventory.Contains(key.ItemDefinition))
                {
                    key.ApplyCollectedState();
                }
            }
        }
    }
}
