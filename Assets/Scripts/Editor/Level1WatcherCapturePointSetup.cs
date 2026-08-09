#if UNITY_EDITOR
using NHNHackathon.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1WatcherCapturePointSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Assign Watcher Player Capture Points")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int configured = 0;
            int created = 0;
            foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                WatcherCapturePresenter presenter =
                    enemy.GetComponentInChildren<WatcherCapturePresenter>(true)
                    ?? enemy.gameObject.AddComponent<WatcherCapturePresenter>();
                Transform point = enemy.transform.Find("PlayerCapturePoint");
                if (point == null)
                {
                    point = new GameObject("PlayerCapturePoint").transform;
                    point.SetParent(enemy.transform, false);
                    point.localPosition = new Vector3(0f, 0f, 0.9f);
                    point.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    point.localScale = Vector3.one;
                    created++;
                }

                SerializedObject values = new(presenter);
                values.FindProperty("playerCapturePoint").objectReferenceValue = point;
                values.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
                configured++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"LEVEL1_WATCHER_CAPTURE_POINTS_COMPLETE configured={configured} created={created}");
        }
    }
}
#endif
