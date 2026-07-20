using UnityEngine;

namespace OnlyOnePlayer.Prototype.Mission
{
    public sealed class DataExtractionZone2D : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color gizmoColor = new(0.1f, 0.45f, 1f, 0.25f);
        [SerializeField] private Vector2 gizmoSize = new(4f, 2.5f);

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, gizmoSize);
        }
    }
}
