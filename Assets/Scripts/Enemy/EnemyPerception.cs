using UnityEngine;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyPerception : MonoBehaviour
    {
        [Header("Sight")]
        [SerializeField, Min(0f)] private float sightDistance = 10f;
        [SerializeField, Range(0f, 360f)] private float fieldOfViewAngle = 90f;
        [SerializeField, Min(0f)] private float eyeHeight = 1.6f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        public float SightDistance => sightDistance;

        public bool CanSeeTarget(Transform target)
        {
            return target != null && CanSeePoint(target.position + Vector3.up, true);
        }

        public bool CanSeePoint(Vector3 point, bool requireFieldOfView)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 toPoint = point - origin;
            if (toPoint.sqrMagnitude > sightDistance * sightDistance)
            {
                return false;
            }

            if (requireFieldOfView && Vector3.Angle(transform.forward, toPoint) > fieldOfViewAngle * 0.5f)
            {
                return false;
            }

            return !Physics.Linecast(origin, point, obstructionMask, QueryTriggerInteraction.Ignore);
        }

        public bool HasClearAttackPath(Transform target, float distance, float maximumHeightDifference)
        {
            if (target == null || Vector3.Distance(transform.position, target.position) > distance
                || Mathf.Abs(transform.position.y - target.position.y) > maximumHeightDifference)
            {
                return false;
            }

            return CanSeePoint(target.position + Vector3.up, false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Gizmos.DrawWireSphere(origin, sightDistance);
            Quaternion left = Quaternion.Euler(0f, -fieldOfViewAngle * 0.5f, 0f);
            Quaternion right = Quaternion.Euler(0f, fieldOfViewAngle * 0.5f, 0f);
            Gizmos.DrawRay(origin, left * transform.forward * sightDistance);
            Gizmos.DrawRay(origin, right * transform.forward * sightDistance);
        }
    }
}
