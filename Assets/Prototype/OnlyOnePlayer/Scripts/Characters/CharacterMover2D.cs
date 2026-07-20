using OnlyOnePlayer.Prototype.Utilities;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Characters
{
    public sealed class CharacterMover2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;

        [Header("Physics")]
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private Collider2D characterCollider;

        [Header("Status")]
        [SerializeField] private CharacterStatus characterStatus;
        [SerializeField] private CharacterIdentity characterIdentity;

        [Header("Camera Bounds")]
        [SerializeField] private bool keepInsideCameraView = true;
        [SerializeField, Min(0f)] private float cameraBoundsPadding = 0.45f;

        private Vector2 moveInput;

        public void SetMoveInput(Vector2 input)
        {
            moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        }

        public void Configure(float speed, Rigidbody2D rigidbody2D)
        {
            moveSpeed = Mathf.Max(0f, speed);
            targetRigidbody = rigidbody2D;
            SetupRigidbody();
        }

        private void Reset()
        {
            targetRigidbody = GetComponent<Rigidbody2D>();
            characterCollider = GetComponent<Collider2D>();
            characterStatus = GetComponent<CharacterStatus>();
            characterIdentity = GetComponent<CharacterIdentity>();
        }

        private void Awake()
        {
            SetupRigidbody();
        }

        private void OnDestroy()
        {
            CharacterCollisionRegistry2D.Unregister(characterCollider);
        }

        private void FixedUpdate()
        {
            if (characterStatus != null && characterStatus.IsStunned)
            {
                targetRigidbody.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 nextPosition = targetRigidbody.position + moveInput * moveSpeed * Time.fixedDeltaTime;

            if (keepInsideCameraView)
            {
                nextPosition = CameraBoundsUtility2D.ClampToMainCamera(nextPosition, cameraBoundsPadding);
            }

            targetRigidbody.MovePosition(nextPosition);
        }

        private void SetupRigidbody()
        {
            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponent<Rigidbody2D>();
            }

            if (targetRigidbody == null)
            {
                targetRigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            if (characterCollider == null)
            {
                characterCollider = GetComponent<Collider2D>();
            }

            if (characterCollider == null)
            {
                characterCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            characterCollider.isTrigger = false;

            if (characterStatus == null)
            {
                characterStatus = GetComponent<CharacterStatus>();
            }

            if (characterStatus == null)
            {
                characterStatus = gameObject.AddComponent<CharacterStatus>();
            }

            if (characterIdentity == null)
            {
                characterIdentity = GetComponent<CharacterIdentity>();
            }

            if (characterIdentity == null)
            {
                characterIdentity = gameObject.AddComponent<CharacterIdentity>();
            }

            if (characterIdentity.ActorType == CharacterActorType.Unknown)
            {
                characterIdentity.Configure(GetComponent<RealPlayerIdentity>() != null ? CharacterActorType.RealPlayer : CharacterActorType.Npc, name);
            }

            targetRigidbody.gravityScale = 0f;
            targetRigidbody.freezeRotation = true;
            CharacterCollisionRegistry2D.Register(characterCollider);
        }
    }
}
