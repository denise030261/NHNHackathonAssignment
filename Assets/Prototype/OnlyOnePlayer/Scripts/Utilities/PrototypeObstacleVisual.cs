using UnityEngine;

namespace OnlyOnePlayer.Prototype.Utilities
{
    [ExecuteAlways]
    public sealed class PrototypeObstacleVisual : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Color obstacleColor = Color.gray;
        [SerializeField] private Vector2 size = Vector2.one;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D boxCollider;

        private static Sprite fallbackSprite;
#if UNITY_EDITOR
        private bool hasPendingEditorApply;
#endif

        public void Configure(Color color, Vector2 visualSize, SpriteRenderer renderer, BoxCollider2D collider2D)
        {
            obstacleColor = color;
            size = new Vector2(Mathf.Max(0.1f, visualSize.x), Mathf.Max(0.1f, visualSize.y));
            spriteRenderer = renderer;
            boxCollider = collider2D;
            ApplyVisual();
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
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

            spriteRenderer.sprite = defaultSprite != null ? defaultSprite : GetFallbackSprite();
            spriteRenderer.color = obstacleColor;
            transform.localScale = new Vector3(size.x, size.y, 1f);

            if (boxCollider != null)
            {
                boxCollider.size = Vector2.one;
            }
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
                name = "Prototype Obstacle Square Texture",
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            fallbackSprite.name = "Prototype Obstacle Square Sprite";
            fallbackSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackSprite;
        }
    }
}
