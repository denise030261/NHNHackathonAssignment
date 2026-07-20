using UnityEngine;

namespace OnlyOnePlayer.Prototype.Input
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Keys")]
        [SerializeField] private KeyCode upKey = KeyCode.W;
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode downKey = KeyCode.S;
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Header("Input Options")]
        [SerializeField] private bool normalizeDiagonalInput = true;

        public Vector2 MoveInput { get; private set; }

        private void Update()
        {
            MoveInput = ReadMoveInput();
        }

        private Vector2 ReadMoveInput()
        {
            var input = Vector2.zero;

            if (UnityEngine.Input.GetKey(upKey))
            {
                input.y += 1f;
            }

            if (UnityEngine.Input.GetKey(downKey))
            {
                input.y -= 1f;
            }

            if (UnityEngine.Input.GetKey(leftKey))
            {
                input.x -= 1f;
            }

            if (UnityEngine.Input.GetKey(rightKey))
            {
                input.x += 1f;
            }

            return normalizeDiagonalInput && input.sqrMagnitude > 1f ? input.normalized : input;
        }
    }
}
