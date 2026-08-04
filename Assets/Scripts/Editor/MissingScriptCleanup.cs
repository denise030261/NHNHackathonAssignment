#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class MissingScriptCleanup
    {
        [MenuItem("Tools/NHN Hackathon/Maintenance/Find Missing Scripts")]
        public static void Find()
        {
            int count = ScanAndOptionallyClean(false);
            Debug.Log($"Missing script scan complete. Found {count} component(s).");
        }

        [MenuItem("Tools/NHN Hackathon/Maintenance/Remove Missing Scripts")]
        public static void Clean()
        {
            int count = ScanAndOptionallyClean(true);
            Debug.Log($"MISSING_SCRIPT_CLEANUP_COMPLETE: Removed {count} component(s).");
        }

        private static int ScanAndOptionallyClean(bool clean)
        {
            int total = 0;
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (string sceneGuid in scenePaths)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int sceneCount = ProcessHierarchy(scene.GetRootGameObjects(), scenePath, clean);
                total += sceneCount;
                if (clean && sceneCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, scenePath);
                }
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    int prefabCount = ProcessHierarchy(new[] { root }, prefabPath, clean);
                    total += prefabCount;
                    if (clean && prefabCount > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            if (clean)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return total;
        }

        private static int ProcessHierarchy(
            IEnumerable<GameObject> roots, string assetPath, bool clean)
        {
            int total = 0;
            foreach (GameObject root in roots)
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject target = transform.gameObject;
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
                    if (missingCount <= 0)
                    {
                        continue;
                    }

                    total += missingCount;
                    Debug.LogWarning(
                        $"Missing script: {assetPath} / {GetHierarchyPath(target.transform)} " +
                        $"({missingCount} component(s))");
                    if (clean)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
                    }
                }
            }
            return total;
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }
            return path;
        }
    }
}
#endif
