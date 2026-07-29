using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.Inspection
{
    [DisallowMultipleComponent]
    public sealed class ItemPreviewRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera previewCamera;

        [Header("Preview")]
        [SerializeField, Min(64)] private int textureSize = 512;
        [SerializeField, Min(0.1f)] private float cameraDistance = 3f;
        [SerializeField, Min(1f)] private float rotationSensitivity = 0.35f;
        [SerializeField] private Color backgroundColor = new Color(0.015f, 0.015f, 0.02f, 1f);

        private const int PreviewLayer = 31;
        private RenderTexture renderTexture;
        private GameObject previewInstance;
        private Transform previewPivot;
        private Vector3 initialEulerAngles;

        public RenderTexture Texture => renderTexture;

        private void Awake()
        {
            EnsureResources();
        }

        public void Show(ItemDefinition item)
        {
            Clear();
            EnsureResources();
            if (item == null || item.PreviewPrefab == null)
            {
                return;
            }

            previewInstance = Instantiate(item.PreviewPrefab, previewPivot);
            previewInstance.name = $"{item.DisplayName}_Preview";
            previewInstance.transform.SetLocalPositionAndRotation(
                Vector3.zero, Quaternion.Euler(item.PreviewEulerAngles));
            previewInstance.transform.localScale *= item.PreviewScale;
            SetLayerRecursively(previewInstance.transform, PreviewLayer);
            DisablePreviewBehaviours(previewInstance);
            initialEulerAngles = previewPivot.localEulerAngles;
        }

        public void Rotate(Vector2 mouseDelta)
        {
            if (previewPivot == null)
            {
                return;
            }

            previewPivot.Rotate(Vector3.up, -mouseDelta.x * rotationSensitivity, Space.World);
            previewPivot.Rotate(Vector3.right, mouseDelta.y * rotationSensitivity, Space.World);
        }

        public void ResetRotation()
        {
            if (previewPivot != null)
            {
                previewPivot.localRotation = Quaternion.Euler(initialEulerAngles);
            }
        }

        public void Clear()
        {
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
        }

        private void EnsureResources()
        {
            if (previewPivot == null)
            {
                GameObject pivotObject = new GameObject("PreviewPivot");
                previewPivot = pivotObject.transform;
                previewPivot.SetParent(transform, false);
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(textureSize, textureSize, 24)
                {
                    name = "ItemInspectionRenderTexture",
                    antiAliasing = 2
                };
                renderTexture.Create();
            }

            if (previewCamera != null)
            {
                previewCamera.targetTexture = renderTexture;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = backgroundColor;
                previewCamera.cullingMask = 1 << PreviewLayer;
                previewCamera.transform.localPosition = new Vector3(0f, 0f, -cameraDistance);
                previewCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private static void DisablePreviewBehaviours(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Collider itemCollider in root.GetComponentsInChildren<Collider>(true))
            {
                itemCollider.enabled = false;
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private void OnDestroy()
        {
            Clear();
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}
