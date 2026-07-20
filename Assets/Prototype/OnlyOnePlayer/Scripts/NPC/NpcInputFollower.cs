using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Input;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.NPC
{
    [RequireComponent(typeof(CharacterMover2D))]
    public sealed class NpcInputFollower : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private CharacterMover2D mover;

        [Header("NPC Follow Rule")]
        [SerializeField] private NpcFollowType followType = NpcFollowType.Same;

        [Header("Delay Settings")]
        [SerializeField, Min(0f)] private float inputDelay = 0.5f;

        [Header("Ignore Settings")]
        [SerializeField, Range(0f, 1f)] private float ignoreInputChance = 0.1f;

        private readonly Queue<DelayedInput> delayedInputs = new();

        public void Configure(PlayerInputReader reader, CharacterMover2D characterMover, NpcFollowType type, float delay)
        {
            Configure(reader, characterMover, type, delay, ignoreInputChance);
        }

        public void Configure(PlayerInputReader reader, CharacterMover2D characterMover, NpcFollowType type, float delay, float ignoreChance)
        {
            inputReader = reader;
            mover = characterMover;
            followType = type;
            inputDelay = Mathf.Max(0f, delay);
            ignoreInputChance = Mathf.Clamp01(ignoreChance);
            delayedInputs.Clear();
        }

        private void Reset()
        {
            mover = GetComponent<CharacterMover2D>();
        }

        private void Awake()
        {
            if (mover == null)
            {
                mover = GetComponent<CharacterMover2D>();
            }
        }

        private void Update()
        {
            if (inputReader == null)
            {
                mover.SetMoveInput(Vector2.zero);
                return;
            }

            Vector2 input = inputReader.MoveInput;
            Vector2 transformedInput = followType == NpcFollowType.Delayed
                ? GetDelayedInput(input)
                : TransformInput(input);

            mover.SetMoveInput(transformedInput);
        }

        private Vector2 TransformInput(Vector2 input)
        {
            return followType switch
            {
                NpcFollowType.Same => input,
                NpcFollowType.Slow80 => input,
                NpcFollowType.Ignore10 => Random.value < ignoreInputChance ? Vector2.zero : input,
                NpcFollowType.InvertW => input.y > 0f ? new Vector2(input.x, -input.y) : input,
                NpcFollowType.InvertA => input.x < 0f ? new Vector2(-input.x, input.y) : input,
                NpcFollowType.InvertS => input.y < 0f ? new Vector2(input.x, -input.y) : input,
                NpcFollowType.InvertD => input.x > 0f ? new Vector2(-input.x, input.y) : input,
                NpcFollowType.InvertAll => -input,
                _ => input
            };
        }

        private Vector2 GetDelayedInput(Vector2 currentInput)
        {
            delayedInputs.Enqueue(new DelayedInput(Time.time, currentInput));

            Vector2 result = Vector2.zero;
            while (delayedInputs.Count > 0 && Time.time - delayedInputs.Peek().TimeStamp >= inputDelay)
            {
                result = delayedInputs.Dequeue().Input;
            }

            return result;
        }

        private readonly struct DelayedInput
        {
            public DelayedInput(float timeStamp, Vector2 input)
            {
                TimeStamp = timeStamp;
                Input = input;
            }

            public float TimeStamp { get; }
            public Vector2 Input { get; }
        }
    }
}
