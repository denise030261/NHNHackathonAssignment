using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class RuleViolationReporter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WatcherController2D[] watchers;
        [SerializeField] private RealPlayerIdentity realPlayer;

        [Header("Keyboard Test")]
        [SerializeField] private bool enableKeyboardTest = true;
        [SerializeField] private KeyCode reportViolationKey = KeyCode.Space;

        public void ReportViolation(RealPlayerIdentity target)
        {
            if (watchers == null)
            {
                return;
            }

            foreach (WatcherController2D watcher in watchers)
            {
                if (watcher != null)
                {
                    watcher.ReportRuleViolation(target);
                }
            }
        }

        private void Update()
        {
            if (!enableKeyboardTest || realPlayer == null)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(reportViolationKey))
            {
                ReportViolation(realPlayer);
            }
        }
    }
}
