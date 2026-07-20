using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class CheckpointGate2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CheckpointAccessController2D accessController;
        [SerializeField] private ForbiddenActionReporter reporter;
        [SerializeField] private Collider2D triggerCollider;

        private void Awake()
        {
            if (accessController == null)
            {
                accessController = GetComponentInParent<CheckpointAccessController2D>();
            }

            if (reporter == null)
            {
                reporter = FindAnyObjectByType<ForbiddenActionReporter>();
            }

            SetupTrigger();
        }

        private void OnEnable()
        {
            SetupTrigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            if (accessController != null && accessController.HasPermission(actor))
            {
                accessController.MarkPassed(actor);
                return;
            }

            reporter?.Report(actor, ForbiddenActionType.BypassingCheckpointGate);
        }

        private void Reset()
        {
            accessController = GetComponentInParent<CheckpointAccessController2D>();
            triggerCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void SetupTrigger()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            triggerCollider.isTrigger = true;
        }
    }
}
