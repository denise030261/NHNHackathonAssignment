using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NHNHackathon.Interaction
{
    [DisallowMultipleComponent]
    public sealed class InteractionOutline : MonoBehaviour
    {
        private const string ShaderResourceName = "InteractionOutline";
        private static readonly int OutlineColorId =
            Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlinePixelsId =
            Shader.PropertyToID("_OutlinePixels");

        private static Material sharedOutlineMaterial;

        private readonly List<RendererPair> rendererPairs = new();
        private GameObject outlineRoot;
        private MaterialPropertyBlock propertyBlock;
        private Color outlineColor = new(1f, 0.78f, 0.2f, 1f);
        private float outlinePixels = 4f;
        private bool isHighlighted;

        public void Configure(Color color, float pixels)
        {
            outlineColor = color;
            outlinePixels = Mathf.Clamp(pixels, 0.5f, 12f);
            ApplyProperties();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
            {
                EnsureOutlineObjects();
            }

            isHighlighted = highlighted;
            if (outlineRoot != null)
            {
                outlineRoot.SetActive(highlighted);
            }
            foreach (RendererPair pair in rendererPairs)
            {
                if (pair.Outline != null)
                {
                    pair.Outline.enabled = highlighted
                        && pair.Source != null
                        && pair.Source.enabled;
                }
            }
        }

        private void LateUpdate()
        {
            if (!isHighlighted)
            {
                return;
            }

            foreach (RendererPair pair in rendererPairs)
            {
                if (pair.Source != null && pair.Outline != null)
                {
                    pair.Outline.enabled = pair.Source.enabled;
                }
            }
        }

        private void EnsureOutlineObjects()
        {
            if (outlineRoot != null || !EnsureSharedMaterial())
            {
                return;
            }

            outlineRoot = new GameObject("InteractionOutline_Renderers")
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            outlineRoot.transform.SetParent(transform, false);

            Renderer[] sources = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer source in sources)
            {
                if (source is MeshRenderer meshRenderer)
                {
                    CreateMeshOutline(meshRenderer);
                }
                else if (source is SkinnedMeshRenderer skinnedRenderer)
                {
                    CreateSkinnedOutline(skinnedRenderer);
                }
            }

            ApplyProperties();
        }

        private void CreateMeshOutline(MeshRenderer source)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return;
            }

            GameObject clone = CreateRendererObject(source.transform);
            MeshFilter cloneFilter = clone.AddComponent<MeshFilter>();
            cloneFilter.sharedMesh = sourceFilter.sharedMesh;
            MeshRenderer outline = clone.AddComponent<MeshRenderer>();
            CopyRendererSettings(source, outline);
            AssignOutlineMaterials(source, outline);
            rendererPairs.Add(new RendererPair(source, outline));
        }

        private void CreateSkinnedOutline(SkinnedMeshRenderer source)
        {
            if (source.sharedMesh == null)
            {
                return;
            }

            GameObject clone = CreateRendererObject(source.transform);
            SkinnedMeshRenderer outline = clone.AddComponent<SkinnedMeshRenderer>();
            outline.sharedMesh = source.sharedMesh;
            outline.rootBone = source.rootBone;
            outline.bones = source.bones;
            outline.localBounds = source.localBounds;
            outline.quality = source.quality;
            outline.updateWhenOffscreen = source.updateWhenOffscreen;
            for (int index = 0; index < source.sharedMesh.blendShapeCount; index++)
            {
                outline.SetBlendShapeWeight(index, source.GetBlendShapeWeight(index));
            }
            CopyRendererSettings(source, outline);
            AssignOutlineMaterials(source, outline);
            rendererPairs.Add(new RendererPair(source, outline));
        }

        private GameObject CreateRendererObject(Transform source)
        {
            GameObject clone = new($"{source.name}_Outline")
            {
                layer = source.gameObject.layer,
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            clone.transform.SetParent(source, false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;
            return clone;
        }

        private static void CopyRendererSettings(Renderer source, Renderer target)
        {
            target.enabled = source.enabled;
            target.shadowCastingMode = ShadowCastingMode.Off;
            target.receiveShadows = false;
            target.lightProbeUsage = LightProbeUsage.Off;
            target.reflectionProbeUsage = ReflectionProbeUsage.Off;
            target.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            target.allowOcclusionWhenDynamic = false;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder + 1;
        }

        private static void AssignOutlineMaterials(Renderer source, Renderer target)
        {
            int materialCount = Mathf.Max(1, source.sharedMaterials.Length);
            Material[] materials = new Material[materialCount];
            for (int index = 0; index < materials.Length; index++)
            {
                materials[index] = sharedOutlineMaterial;
            }
            target.sharedMaterials = materials;
        }

        private void ApplyProperties()
        {
            if (rendererPairs.Count == 0)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.SetColor(OutlineColorId, outlineColor);
            propertyBlock.SetFloat(OutlinePixelsId, outlinePixels);
            foreach (RendererPair pair in rendererPairs)
            {
                if (pair.Outline != null)
                {
                    pair.Outline.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private static bool EnsureSharedMaterial()
        {
            if (sharedOutlineMaterial != null)
            {
                return true;
            }

            Shader shader = Resources.Load<Shader>(ShaderResourceName);
            if (shader == null)
            {
                Debug.LogError(
                    $"Interaction outline shader was not found in Resources/{ShaderResourceName}.");
                return false;
            }

            sharedOutlineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return true;
        }

        private void OnDestroy()
        {
            foreach (RendererPair pair in rendererPairs)
            {
                if (pair.Outline != null)
                {
                    Destroy(pair.Outline.gameObject);
                }
            }
            if (outlineRoot != null)
            {
                Destroy(outlineRoot);
            }
        }

        private readonly struct RendererPair
        {
            public RendererPair(Renderer source, Renderer outline)
            {
                Source = source;
                Outline = outline;
            }

            public Renderer Source { get; }
            public Renderer Outline { get; }
        }
    }
}
