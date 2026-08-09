#if UNITY_EDITOR
using NHNHackathon.Dance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class DanceZoneAutomaticCameraRemovalSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Interactables/DanceSyncZone.prefab";
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Remove DanceZone Automatic Camera")]
        public static void Build()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform cameraPoint = prefab.transform.Find("DanceCameraPoint");
                if (cameraPoint != null)
                {
                    Object.DestroyImmediate(cameraPoint.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            foreach (DanceZoneCameraTrigger trigger in
                     Object.FindObjectsByType<DanceZoneCameraTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform cameraPoint = trigger.transform.Find("DanceCameraPoint");
                if (cameraPoint != null
                    && !PrefabUtility.IsPartOfPrefabInstance(cameraPoint.gameObject))
                {
                    Object.DestroyImmediate(cameraPoint.gameObject);
                }
                EditorUtility.SetDirty(trigger);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DANCE_ZONE_AUTOMATIC_CAMERA_REMOVED: Perspective switching remains enabled.");
        }
    }
}
#endif
