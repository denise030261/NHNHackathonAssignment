using UnityEngine;
using UnityEngine.EventSystems;

namespace NHNHackathon.Inspection
{
    [DisallowMultipleComponent]
    public sealed class ItemPreviewDragHandler : MonoBehaviour, IDragHandler
    {
        [SerializeField] private ItemPreviewRenderer previewRenderer;
        [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.35f;

        public void OnDrag(PointerEventData eventData)
        {
            if (previewRenderer != null
                && eventData.button == PointerEventData.InputButton.Left)
            {
                previewRenderer.Rotate(eventData.delta * rotationSensitivity);
            }
        }
    }
}
