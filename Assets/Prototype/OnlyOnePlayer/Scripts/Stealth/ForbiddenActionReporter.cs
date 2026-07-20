using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class ForbiddenActionReporter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WatcherController2D[] watchers;

        private void Awake()
        {
            if (watchers == null || watchers.Length == 0)
            {
                watchers = FindObjectsByType<WatcherController2D>(FindObjectsSortMode.None);
            }
        }

        public void Report(CharacterIdentity actor, ForbiddenActionType actionType)
        {
            if (actor == null || watchers == null)
            {
                return;
            }

            foreach (WatcherController2D watcher in watchers)
            {
                if (watcher != null)
                {
                    watcher.ReportForbiddenAction(actor, actionType);
                }
            }
        }
    }
}
