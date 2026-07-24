using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    public sealed class KeyCollectibleVisual : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField, Min(0f)] private float bobHeight = 0.12f;
        [SerializeField, Min(0f)] private float bobSpeed = 2f;

        private Vector3 initialLocalPosition;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            Vector3 position = initialLocalPosition;
            position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = position;
        }
    }
}
