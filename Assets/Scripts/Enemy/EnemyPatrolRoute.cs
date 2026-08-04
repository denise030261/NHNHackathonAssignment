using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyPatrolRoute : MonoBehaviour
    {
        [SerializeField] private List<Transform> points = new List<Transform>();

        public int Count => points.Count;

        public Transform GetPoint(int index)
        {
            return points.Count == 0 ? null : points[Mathf.Abs(index) % points.Count];
        }

        public int FindNearestPointIndex(Vector3 position)
        {
            int nearestIndex = 0;
            float nearestDistanceSqr = float.PositiveInfinity;
            for (int index = 0; index < points.Count; index++)
            {
                Transform point = points[index];
                if (point == null)
                {
                    continue;
                }

                float distanceSqr = (point.position - position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestIndex = index;
                }
            }
            return nearestIndex;
        }
    }
}
