using OnlyOnePlayer.Prototype.Input;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Characters
{
    [RequireComponent(typeof(CharacterMover2D))]
    public sealed class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private CharacterMover2D mover;

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

            mover.SetMoveInput(inputReader.MoveInput);
        }
    }
}
