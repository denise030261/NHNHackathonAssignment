using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    public sealed class DanceColorVisualizer : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Shared Dance Data")]
        [SerializeField] private DanceCatalog danceCatalog;

        [Header("References")]
        [SerializeField] private Renderer targetRenderer;

        [Header("Default Appearance")]
        [SerializeField] private Color defaultColor = Color.white;

        private MaterialPropertyBlock propertyBlock;
        private float resetAtTime = float.PositiveInfinity;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            ApplyColor(defaultColor);
        }

        private void Update()
        {
            if (Time.time >= resetAtTime)
            {
                ApplyColor(defaultColor);
                resetAtTime = float.PositiveInfinity;
            }
        }

        public bool ShowDance(int danceId, float duration = -1f)
        {
            if (danceCatalog == null || !danceCatalog.TryGetDance(danceId, out DanceDefinition dance))
            {
                Debug.LogWarning($"Dance ID {danceId} is not present in the assigned catalog.", this);
                return false;
            }

            ApplyColor(dance.DisplayColor);
            resetAtTime = duration >= 0f ? Time.time + duration : float.PositiveInfinity;
            return true;
        }

        public void ShowDance(DanceDefinition dance, float duration = -1f)
        {
            if (dance == null)
            {
                return;
            }

            ApplyColor(dance.DisplayColor);
            resetAtTime = duration >= 0f ? Time.time + duration : float.PositiveInfinity;
        }

        public void ResetColor()
        {
            ApplyColor(defaultColor);
            resetAtTime = float.PositiveInfinity;
        }

        private void ApplyColor(Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void OnValidate()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
        }
    }
}
