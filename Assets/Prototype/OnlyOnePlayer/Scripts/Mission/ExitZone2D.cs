using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Stealth;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Mission
{
    public sealed class ExitZone2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ConfidentialDataMissionController missionController;
        [SerializeField] private ForbiddenActionReporter forbiddenActionReporter;
        [SerializeField] private Collider2D triggerCollider;

        [Header("Exit")]
        [SerializeField] private ExitType exitType = ExitType.Real;

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null)
            {
                return;
            }

            if (exitType == ExitType.Fake)
            {
                forbiddenActionReporter?.Report(actor, ForbiddenActionType.EnteringFakeExit);
                missionController?.EnterExit(exitType);
                return;
            }

            RealPlayerIdentity player = other.GetComponent<RealPlayerIdentity>();
            if (player == null || (missionController != null && player != missionController.RealPlayer))
            {
                return;
            }

            missionController?.EnterExit(exitType);
        }

        private void Awake()
        {
            if (missionController == null)
            {
                missionController = FindAnyObjectByType<ConfidentialDataMissionController>();
            }

            if (forbiddenActionReporter == null)
            {
                forbiddenActionReporter = FindAnyObjectByType<ForbiddenActionReporter>();
            }

            SetupTrigger();
        }

        private void OnEnable()
        {
            SetupTrigger();
        }

        private void Reset()
        {
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
