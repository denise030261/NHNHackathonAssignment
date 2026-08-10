using UnityEngine;

namespace NHNHackathon.Characters
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CharacterMaterialApplicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Only renderers below this transform receive the character material.")]
        private Transform rendererRoot;

        [SerializeField] private Material characterMaterial;

        private void OnEnable()
        {
            ApplyMaterial();
        }

        private void OnValidate()
        {
            ApplyMaterial();
        }

        [ContextMenu("Apply Character Material")]
        public void ApplyMaterial()
        {
            if (rendererRoot == null || characterMaterial == null)
            {
                return;
            }

            Renderer[] renderers = rendererRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer targetRenderer in renderers)
            {
                Material[] materials = targetRenderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    targetRenderer.sharedMaterial = characterMaterial;
                    continue;
                }

                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] == characterMaterial)
                    {
                        continue;
                    }

                    materials[index] = characterMaterial;
                    changed = true;
                }

                if (changed)
                {
                    targetRenderer.sharedMaterials = materials;
                }
            }
        }
    }
}
