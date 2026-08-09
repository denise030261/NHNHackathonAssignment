#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.AudioSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class GameSfxPoolSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Build SFX Audio Pool")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject audioRoot = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(value => value.gameObject)
                .FirstOrDefault(value => value.name == "Audio");
            if (audioRoot == null)
            {
                audioRoot = new GameObject("Audio");
            }

            Transform existing = audioRoot.transform.Find("SFX Pool");
            GameObject poolObject;
            if (existing != null)
            {
                poolObject = existing.gameObject;
            }
            else
            {
                poolObject = new GameObject("SFX Pool");
                poolObject.transform.SetParent(audioRoot.transform, false);
            }
            if (poolObject.GetComponent<GameSfxPool>() == null)
            {
                poolObject.AddComponent<GameSfxPool>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("GAME_SFX_POOL_COMPLETE: 12 reusable 3D sources, maximum 32.");
        }
    }
}
#endif
