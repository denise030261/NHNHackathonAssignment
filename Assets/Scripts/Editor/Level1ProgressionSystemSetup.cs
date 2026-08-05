#if UNITY_EDITOR
using NHNHackathon.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1ProgressionSystemSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Ensure Progression System")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameProgressionController controller =
                Object.FindAnyObjectByType<GameProgressionController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                GameObject system = new("ProgressionSystem");
                controller = system.AddComponent<GameProgressionController>();
            }
            else
            {
                controller.gameObject.name = "ProgressionSystem";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_PROGRESSION_SYSTEM_COMPLETE");
        }
    }
}
#endif
