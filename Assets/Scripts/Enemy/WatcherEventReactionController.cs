using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    public sealed class WatcherEventReactionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform unlockReactionTarget;
        [SerializeField] private Transform crowdTarget;

        [Header("First Unlock Reaction")]
        [SerializeField, Min(0.01f)] private float turnDuration = 0.35f;
        [SerializeField, Min(0f)] private float reactionDuration = 2f;

        private Sequence reactionSequence;
        private bool controllerWasEnabled;

        public void ReactToFirstUnlock()
        {
            if (unlockReactionTarget == null) return;
            StopReaction();
            controllerWasEnabled = enemyController != null && enemyController.enabled;
            if (enemyController != null) enemyController.enabled = false;
            StopAgent();
            reactionSequence = DOTween.Sequence().SetLink(gameObject);
            reactionSequence.Append(transform.DORotateQuaternion(GetFlatLookRotation(unlockReactionTarget.position), turnDuration));
            reactionSequence.AppendInterval(reactionDuration);
            reactionSequence.OnComplete(RestorePatrol);
        }

        public void FocusOnCrowd()
        {
            if (crowdTarget == null) return;
            StopReaction();
            if (enemyController != null) enemyController.enabled = false;
            StopAgent();
            reactionSequence = DOTween.Sequence().SetLink(gameObject);
            reactionSequence.Append(transform.DORotateQuaternion(GetFlatLookRotation(crowdTarget.position), turnDuration));
        }

        private Quaternion GetFlatLookRotation(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : transform.rotation;
        }

        private void StopAgent()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private void RestorePatrol()
        {
            reactionSequence = null;
            if (enemyController != null) enemyController.enabled = controllerWasEnabled;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }

        private void StopReaction()
        {
            reactionSequence?.Kill();
            reactionSequence = null;
        }

        private void OnDisable() => StopReaction();
    }
}
