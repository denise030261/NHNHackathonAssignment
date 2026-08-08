using System.Collections.Generic;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.SaveSystem
{
    public sealed class CheckpointSnapshot
    {
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetForNewGame();
        }

        public static void ResetForNewGame()
        {
            snapshot = null;
            respawnRequested = false;
        }

        public static void RegisterInitialSpawn(PlayerCheckpointAgent player)
        {
            if (snapshot != null || player == null)
            {
                return;
            }

            snapshot = new CheckpointSnapshot
            {
                Position = player.transform.position,
                Rotation = player.transform.rotation
            };
            player.WriteProgressTo(snapshot);
        }

        public static void SaveCheckpoint(
            PlayerCheckpointAgent player, Vector3 position, Quaternion rotation)
        {
            if (player == null)
            {
                return;
            }

            snapshot ??= new CheckpointSnapshot();
            snapshot.Position = position;
            snapshot.Rotation = rotation;
            player.WriteProgressTo(snapshot);
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
            return true;
        }
    }
}
