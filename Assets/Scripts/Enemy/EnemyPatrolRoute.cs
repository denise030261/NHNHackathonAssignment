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
    }
}
