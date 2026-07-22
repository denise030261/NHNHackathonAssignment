using UnityEngine;

namespace NHNHackathon.LightSystem
{
    [DisallowMultipleComponent]
    public sealed class LightStimulusSource : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
        [SerializeField] private Light linkedLight;
        [SerializeField, Tooltip("Surfaces on which the flashlight can create an investigation point.")]
        private LayerMask illuminatedSurfaceMask = 1 << 0;
        [SerializeField, Min(0f), Tooltip("Keeps the navigation hint slightly away from a hit wall.")]
        private float surfaceOffset = 0.4f;

        private Vector3 illuminatedPoint;
        private Vector3 navigationHint;
        private Vector3 previousIlluminatedPoint;

        public bool IsActive => isActiveAndEnabled && (linkedLight == null || linkedLight.enabled);
        public bool IsMoving { get; private set; }
        public Vector3 Position => illuminatedPoint;
        public Vector3 NavigationHint => navigationHint;

        private void OnEnable()
        {
            RefreshIlluminatedPoint();
            previousIlluminatedPoint = illuminatedPoint;
        }

        private void Update()
        {
            RefreshIlluminatedPoint();
            float thresholdSqr = movementThreshold * movementThreshold * Time.deltaTime * Time.deltaTime;
            IsMoving = (illuminatedPoint - previousIlluminatedPoint).sqrMagnitude > thresholdSqr;
            previousIlluminatedPoint = illuminatedPoint;
        }

        private void RefreshIlluminatedPoint()
        {
            float range = linkedLight != null ? linkedLight.range : 10f;
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, illuminatedSurfaceMask,
                    QueryTriggerInteraction.Ignore))
            {
                illuminatedPoint = hit.point;
                navigationHint = hit.point + hit.normal * surfaceOffset;
                return;
            }

            illuminatedPoint = ray.GetPoint(range);
            navigationHint = illuminatedPoint;
        }

        private void OnValidate()
        {
            if (linkedLight == null)
            {
                linkedLight = GetComponent<Light>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            RefreshIlluminatedPoint();
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, illuminatedPoint);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(illuminatedPoint, 0.16f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(navigationHint, 0.2f);
        }
    }
}
