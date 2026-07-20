using UnityEngine;

namespace OnlyOnePlayer.Prototype.Utilities
{
    [ExecuteAlways]
    public sealed class PrototypeCharacterVisual : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Color characterColor = Color.white;
        [SerializeField, Min(0.1f)] private float size = 0.8f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private static Sprite fallbackSprite;
#if UNITY_EDITOR
        private bool hasPendingEditorApply;
#endif

        public void Configure(Color color, float visualSize, SpriteRenderer renderer)
        {
            characterColor = color;
            size = Mathf.Max(0.1f, visualSize);
            spriteRenderer = renderer;
            ApplyVisual();
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
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
            size = Mathf.Max(0.1f, size);

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

            spriteRenderer.sprite = defaultSprite != null ? defaultSprite : GetFallbackSprite();
            spriteRenderer.color = characterColor;
            transform.localScale = Vector3.one * size;
        }

        private static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            fallbackSprite = CreateRuntimeSquareSprite();
            return fallbackSprite;
        }

        private static Sprite CreateRuntimeSquareSprite()
        {
            var texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                name = "Prototype Character Square Texture",
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = "Prototype Character Square Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
