using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Input;
using OnlyOnePlayer.Prototype.Utilities;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.NPC
{
    public sealed class NpcAutoSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform npcRoot;

        [Header("Spawn Settings")]
        [SerializeField] private NpcSpawnDefinition[] spawnDefinitions;
        [SerializeField] private Vector2 spawnAreaCenter;
        [SerializeField] private Vector2 spawnAreaSize = new(8f, 4f);
        [SerializeField] private bool clearPreviouslyGeneratedNpcs = true;

        [Header("NPC Defaults")]
        [SerializeField, Min(0f)] private float npcMoveSpeed = 3f;
        [SerializeField, Min(0f)] private float delayedInputSeconds = 0.5f;
        [SerializeField, Range(0f, 1f)] private float ignoreInputChance = 0.1f;
        [SerializeField, Min(0.1f)] private float npcVisualSize = 0.8f;

        [Header("Random")]
        [SerializeField] private bool useRandomSeed;
        [SerializeField] private int randomSeed = 100;

        [Header("Debug")]
        [SerializeField] private bool drawSpawnAreaGizmo = true;
        [SerializeField] private Color spawnAreaGizmoColor = new(0.2f, 0.8f, 1f, 0.35f);

        private const string GeneratedNpcPrefix = "GeneratedNPC_";
        private const float Slow80SpeedMultiplier = 0.2f;

        private void Start()
        {
            SpawnNpcs();
        }

        public void SpawnNpcs()
        {
            if (inputReader == null)
            {
                Debug.LogWarning("NPC auto spawn skipped because PlayerInputReader is not assigned.", this);
                return;
            }

            Transform root = GetOrCreateRoot();

            if (clearPreviouslyGeneratedNpcs)
            {
                ClearGeneratedNpcs(root);
            }

            if (useRandomSeed)
            {
                Random.InitState(randomSeed);
            }

            if (spawnDefinitions == null)
            {
                return;
            }

            foreach (NpcSpawnDefinition definition in spawnDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                for (int index = 0; index < definition.SpawnCount; index++)
                {
                    CreateNpc(definition, index, root);
                }
            }
        }

        private Transform GetOrCreateRoot()
        {
            if (npcRoot != null)
            {
                return npcRoot;
            }

            var rootObject = new GameObject("NPC_Root");
            npcRoot = rootObject.transform;
            return npcRoot;
        }

        private void ClearGeneratedNpcs(Transform root)
        {
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
                if (!child.name.StartsWith(GeneratedNpcPrefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        private void CreateNpc(NpcSpawnDefinition definition, int index, Transform root)
        {
            var npc = new GameObject($"{GeneratedNpcPrefix}{definition.FollowType}_{index:00}");
            npc.transform.SetParent(root);
            npc.transform.position = GetRandomSpawnPosition();

            var spriteRenderer = npc.AddComponent<SpriteRenderer>();
            var visual = npc.AddComponent<PrototypeCharacterVisual>();
            var body = npc.AddComponent<Rigidbody2D>();
            var collider = npc.AddComponent<BoxCollider2D>();
            var identity = npc.AddComponent<CharacterIdentity>();
            npc.AddComponent<CharacterStatus>();
            var mover = npc.AddComponent<CharacterMover2D>();
            var follower = npc.AddComponent<NpcInputFollower>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            collider.size = Vector2.one;
            identity.Configure(CharacterActorType.Npc, npc.name);

            visual.Configure(definition.Color, npcVisualSize, spriteRenderer);
            mover.Configure(GetMoveSpeed(definition.FollowType), body);
            follower.Configure(inputReader, mover, definition.FollowType, delayedInputSeconds, ignoreInputChance);
        }

        private float GetMoveSpeed(NpcFollowType followType)
        {
            return followType == NpcFollowType.Slow80
                ? npcMoveSpeed * Slow80SpeedMultiplier
                : npcMoveSpeed;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector2 halfSize = spawnAreaSize * 0.5f;
            float x = Random.Range(spawnAreaCenter.x - halfSize.x, spawnAreaCenter.x + halfSize.x);
            float y = Random.Range(spawnAreaCenter.y - halfSize.y, spawnAreaCenter.y + halfSize.y);
            return new Vector3(x, y, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawSpawnAreaGizmo)
            {
                return;
            }

            Gizmos.color = spawnAreaGizmoColor;
            Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        }
    }
}
