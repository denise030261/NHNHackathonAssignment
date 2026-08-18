using System.Collections.Generic;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.SaveSystem
{
    public sealed class CheckpointSnapshot
    {
        public int SaveIndex = int.MinValue;
        public Vector3 Position;
        public Quaternion Rotation;
        public readonly List<ItemDefinition> Items = new();
        public readonly List<ProgressionCondition> Conditions = new();
        public readonly List<int> UnlockedDanceIds = new();
    }

    public static class CheckpointSession
    {
        private static CheckpointSnapshot snapshot;
        private static bool respawnRequested;

        public static bool HasCheckpoint => snapshot != null;
        public static bool LastLoadWasRespawn { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetForNewGame();
        }

        public static void ResetForNewGame()
        {
            snapshot = null;
            respawnRequested = false;
            LastLoadWasRespawn = false;
        }

        public static void RegisterInitialSpawn(PlayerCheckpointAgent player)
        {
            if (snapshot != null || player == null)
            {
                return;
            }

            snapshot = new CheckpointSnapshot
            {
                SaveIndex = int.MinValue,
                Position = player.transform.position,
                Rotation = player.transform.rotation
            };
            player.WriteProgressTo(snapshot);
        }

        public static bool SaveCheckpoint(
            PlayerCheckpointAgent player, int saveIndex,
            Vector3 position, Quaternion rotation)
        {
            if (player == null)
            {
                return false;
            }

            snapshot ??= new CheckpointSnapshot();
            if (saveIndex < snapshot.SaveIndex)
            {
                return false;
            }

            if (saveIndex == snapshot.SaveIndex
                && player.HasSameItems(snapshot.Items))
            {
                return false;
            }

            snapshot.SaveIndex = saveIndex;
            snapshot.Position = position;
            snapshot.Rotation = rotation;
            player.WriteProgressTo(snapshot);
            return true;
        }

        public static void PrepareRespawn(PlayerCheckpointAgent player)
        {
            RegisterInitialSpawn(player);
            if (snapshot != null && player != null)
            {
                // Keep the last checkpoint pose, but preserve progress obtained after reaching it.
                player.WriteProgressTo(snapshot);
            }
            respawnRequested = snapshot != null;
        }

        public static bool TryConsumeRespawn(out CheckpointSnapshot savedSnapshot)
        {
            savedSnapshot = snapshot;
            if (!respawnRequested || snapshot == null)
            {
                return false;
            }

            respawnRequested = false;
            LastLoadWasRespawn = true;
            return true;
        }
    }
}
