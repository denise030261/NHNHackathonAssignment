using OnlyOnePlayer.Prototype.Stealth;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Utilities
{
    [ExecuteAlways]
    public sealed class OneWayZoneVisual2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OneWayZone2D oneWayZone;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private LineRenderer arrowRenderer;

        [Header("Visual")]
        [SerializeField] private Color zoneColor = new Color(0.25f, 0.9f, 0.45f, 0.35f);
        [SerializeField] private Color arrowColor = new Color(0.05f, 1f, 0.25f, 1f);
        [SerializeField] private Vector2 size = new Vector2(2.4f, 1.1f);
        [SerializeField, Min(0.01f)] private float arrowWidth = 0.08f;

        private static Sprite fallbackSprite;
        private static Material fallbackMaterial;
#if UNITY_EDITOR
        private bool hasPendingEditorApply;
#endif

        public void Configure(OneWayZone2D zone, Color color, Vector2 visualSize)
        {
            oneWayZone = zone;
            zoneColor = color;
            size = new Vector2(Mathf.Max(0.1f, visualSize.x), Mathf.Max(0.1f, visualSize.y));
            ApplyVisual();
        }

        private void Reset()
        {
            oneWayZone = GetComponent<OneWayZone2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            arrowRenderer = GetComponentInChildren<LineRenderer>();
        }

        private void Awake()
        {
            ApplyVisual();
        }

        private void OnEnable()
        {
            ApplyVisual();
        }

        private void OnValidate()
        {
            size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));

            if (Application.isPlaying)
            {
                ApplyVisual();
                return;
            }

            ScheduleEditorApply();
        }

        private void ScheduleEditorApply()
        {
#if UNITY_EDITOR
            if (hasPendingEditorApply)
            {
                return;
            }

            hasPendingEditorApply = true;
            UnityEditor.EditorApplication.delayCall += ApplyVisualFromEditorDelay;
#endif
        }

#if UNITY_EDITOR
        private void ApplyVisualFromEditorDelay()
        {
            hasPendingEditorApply = false;

            if (this == null)
            {
                return;
            }

            ApplyVisual();
        }
#endif

        private void ApplyVisual()
        {
            EnsureReferences();

            spriteRenderer.sprite = GetFallbackSprite();
            spriteRenderer.color = zoneColor;
            transform.localScale = new Vector3(size.x, size.y, 1f);

            boxCollider.size = Vector2.one;
            boxCollider.isTrigger = true;

            Vector2 direction = oneWayZone != null ? oneWayZone.AllowedVector : Vector2.right;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector3 direction3D = new Vector3(direction.x, direction.y, 0f);
            Vector3 perpendicular3D = new Vector3(perpendicular.x, perpendicular.y, 0f);
            float shaftLength = 0.55f;
            float headLength = 0.18f;
            float headWidth = 0.16f;

            Vector3 start = -direction3D * shaftLength;
            Vector3 end = direction3D * shaftLength;
            Vector3 headLeft = end - direction3D * headLength + perpendicular3D * headWidth;
            Vector3 headRight = end - direction3D * headLength - perpendicular3D * headWidth;

            arrowRenderer.positionCount = 6;
            arrowRenderer.SetPositions(new[] { start, end, headLeft, end, headRight, end });
            arrowRenderer.startWidth = arrowWidth;
            arrowRenderer.endWidth = arrowWidth;
            arrowRenderer.startColor = arrowColor;
            arrowRenderer.endColor = arrowColor;
            arrowRenderer.useWorldSpace = false;
            arrowRenderer.sortingOrder = 10;
        }

        private void EnsureReferences()
        {
            if (oneWayZone == null)
            {
                oneWayZone = GetComponent<OneWayZone2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }

            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            if (arrowRenderer == null)
            {
                arrowRenderer = GetComponentInChildren<LineRenderer>();
            }

            if (arrowRenderer == null)
            {
                var arrow = new GameObject("DirectionArrow");
                arrow.transform.SetParent(transform, false);
                arrowRenderer = arrow.AddComponent<LineRenderer>();
            }

            arrowRenderer.sharedMaterial = GetFallbackMaterial();
        }

        private static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            var texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                name = "One Way Zone Square Texture",
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            fallbackSprite.name = "One Way Zone Square Sprite";
            fallbackSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackSprite;
        }

        private static Material GetFallbackMaterial()
        {
            if (fallbackMaterial != null)
            {
                return fallbackMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            fallbackMaterial = new Material(shader)
            {
                name = "One Way Zone Arrow Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            return fallbackMaterial;
        }
    }
}
