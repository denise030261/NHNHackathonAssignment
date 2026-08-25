#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    internal static class Level1PerformanceOptimizationSetup
    {
        private const string Level1ScenePath = "Assets/Scenes/Level1.unity";
        private const string CrowdPrefabPath = "Assets/Prefabs/Characters/DancingAI.prefab";

        private const float CrowdCullScreenHeight = 0.035f;
        private const float MinimumOccluderSize = 2.5f;

        [MenuItem("Tools/NHN Hackathon/Optimization/Apply Crowd LOD And Bake Occlusion")]
        private static void ApplyFromMenu()
        {
            RunSetup();
        }

        private static void RunSetup()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != Level1ScenePath)
            {
                Debug.LogError(
                    $"Open {Level1ScenePath} before applying the performance setup.");
                return;
            }

            int crowdRendererCount = ConfigureCrowdPrefab();
            OcclusionSetupResult occlusionResult = ConfigureSceneOcclusion(activeScene);

            if (!EditorSceneManager.SaveScene(activeScene))
            {
                throw new InvalidOperationException(
                    $"Could not save {Level1ScenePath} before baking occlusion data.");
            }

            StaticOcclusionCulling.Clear();
            if (!StaticOcclusionCulling.Compute())
            {
                throw new InvalidOperationException(
                    "Unity failed to bake the Level1 occlusion culling data.");
            }

            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Level1 performance optimization complete. "
                + $"Crowd renderers: {crowdRendererCount}, "
                + $"occludees: {occlusionResult.OccludeeCount}, "
                + $"occluders: {occlusionResult.OccluderCount}, "
                + $"cameras: {occlusionResult.CameraCount}.");
        }

        private static int ConfigureCrowdPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CrowdPrefabPath);
            try
            {
                Animator[] animators = prefabRoot.GetComponentsInChildren<Animator>(true);
                foreach (Animator animator in animators)
                {
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    EditorUtility.SetDirty(animator);
                }

                SkinnedMeshRenderer[] skinnedRenderers =
                    prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skinnedRenderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"No SkinnedMeshRenderer was found in {CrowdPrefabPath}.");
                }

                foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
                {
                    renderer.updateWhenOffscreen = false;
                    renderer.allowOcclusionWhenDynamic = true;
                    renderer.quality = SkinQuality.Bone2;
                    EditorUtility.SetDirty(renderer);
                }

                LODGroup lodGroup = prefabRoot.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = prefabRoot.AddComponent<LODGroup>();
                }

                lodGroup.enabled = true;
                lodGroup.fadeMode = LODFadeMode.None;
                lodGroup.animateCrossFading = false;
                lodGroup.SetLODs(new[]
                {
                    new LOD(CrowdCullScreenHeight, skinnedRenderers.Cast<Renderer>().ToArray())
                });
                lodGroup.RecalculateBounds();
                EditorUtility.SetDirty(lodGroup);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CrowdPrefabPath);
                return skinnedRenderers.Length;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static OcclusionSetupResult ConfigureSceneOcclusion(Scene scene)
        {
            MeshRenderer[] meshRenderers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
                .ToArray();

            int occludeeCount = 0;
            int occluderCount = 0;
            foreach (MeshRenderer renderer in meshRenderers)
            {
                renderer.allowOcclusionWhenDynamic = true;
                EditorUtility.SetDirty(renderer);

                if (HasDynamicObjectInHierarchy(renderer.transform))
                {
                    continue;
                }

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(
                    renderer.gameObject);
                flags |= StaticEditorFlags.OccludeeStatic;
                occludeeCount++;

                if (IsOpaque(renderer) && IsLargeEnoughToOcclude(renderer))
                {
                    flags |= StaticEditorFlags.OccluderStatic;
                    occluderCount++;
                }

                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                EditorUtility.SetDirty(renderer.gameObject);
            }

            Camera[] cameras = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .ToArray();
            foreach (Camera camera in cameras)
            {
                camera.useOcclusionCulling = true;
                EditorUtility.SetDirty(camera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return new OcclusionSetupResult(
                occludeeCount, occluderCount, cameras.Length);
        }

        private static bool HasDynamicObjectInHierarchy(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                GameObject gameObject = current.gameObject;
                if (gameObject.GetComponent<Animator>() != null
                    || gameObject.GetComponent<Animation>() != null
                    || gameObject.GetComponent<Rigidbody>() != null
                    || gameObject.GetComponent<CharacterController>() != null
                    || gameObject.GetComponent<NavMeshAgent>() != null
                    || gameObject.GetComponent<Cloth>() != null
                    || gameObject.GetComponent<ParticleSystem>() != null)
                {
                    return true;
                }

                MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
                if (behaviours.Any(behaviour => behaviour != null))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOpaque(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.renderQueue >= 3000)
                {
                    return false;
                }

                string renderType = material.GetTag("RenderType", false, string.Empty);
                if (renderType.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) >= 0
                    || renderType.IndexOf("Fade", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLargeEnoughToOcclude(Renderer renderer)
        {
            Vector3 size = renderer.bounds.size;
            return Mathf.Max(size.x, size.y, size.z) >= MinimumOccluderSize;
        }

        private readonly struct OcclusionSetupResult
        {
            public OcclusionSetupResult(
                int occludeeCount, int occluderCount, int cameraCount)
            {
                OccludeeCount = occludeeCount;
                OccluderCount = occluderCount;
                CameraCount = cameraCount;
            }

            public int OccludeeCount { get; }
            public int OccluderCount { get; }
            public int CameraCount { get; }
        }
    }
}
#endif
