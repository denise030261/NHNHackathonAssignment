using UnityEngine;

namespace OnlyOnePlayer.Prototype.NPC
{
    [System.Serializable]
    public sealed class NpcSpawnDefinition
    {
        [Header("NPC Type")]
        [SerializeField] private NpcFollowType followType = NpcFollowType.Same;
        [SerializeField, Min(0)] private int spawnCount = 1;

        [Header("Visual")]
        [SerializeField] private Color color = Color.cyan;

        public NpcFollowType FollowType => followType;
        public int SpawnCount => spawnCount;
        public Color Color => color;
    }
}
