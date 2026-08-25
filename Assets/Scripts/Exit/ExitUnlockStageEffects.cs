using NHNHackathon.AudioSystem;
using NHNHackathon.Enemy;
using UnityEngine;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class ExitUnlockStageEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExitDoor exitDoor;
        [SerializeField] private EnemyController watcher;
        [SerializeField] private WatcherEventReactionController watcherReaction;
        [SerializeField] private EnemyPatrolRoute secondStagePatrolRoute;
        [SerializeField] private Light[] corridorLights;
        [SerializeField, Tooltip("Objects disabled on the second unlock (useful for grouped or emissive lights).")]
        private GameObject[] corridorLightObjects;

        [Header("Final Unlock")]
        [SerializeField, Min(0.01f)] private float finalDoorOpenDuration = 4f;

        public void PlayFirstUnlock()
        {
            GameSfxPlayer.PlayFirstExitUnlock(transform.position);
            watcherReaction?.ReactToFirstUnlock();
        }

        public void PlaySecondUnlock()
        {
            if (corridorLights != null)
            {
                foreach (Light corridorLight in corridorLights)
                    if (corridorLight != null) corridorLight.enabled = false;
            }
            if (corridorLightObjects != null)
            {
                foreach (GameObject lightObject in corridorLightObjects)
                    if (lightObject != null) lightObject.SetActive(false);
            }
            if (watcherReaction != null)
            {
                watcherReaction.ReactToSecondUnlock(secondStagePatrolRoute);
            }
            else if (watcher != null && secondStagePatrolRoute != null)
            {
                watcher.SetPatrolRoute(
                    secondStagePatrolRoute, PatrolRouteStartMode.NearestPoint);
            }
        }

        public void PlayFinalUnlock()
        {
            exitDoor?.TryOpen(finalDoorOpenDuration);
            watcherReaction?.ReactToFinalUnlock();
        }
    }
}
