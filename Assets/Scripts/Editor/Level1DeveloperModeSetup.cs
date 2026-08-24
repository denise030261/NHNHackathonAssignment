#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1DeveloperModeSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("NHN Hackathon/Setup/Level1 Developer Mode")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = scene.GetRootGameObjects()
                .FirstOrDefault(value => value.name == "DeveloperMode");
            if (root == null)
            {
                root = new GameObject("DeveloperMode");
            }

            DeveloperModeController controller =
                root.GetComponent<DeveloperModeController>()
                ?? root.AddComponent<DeveloperModeController>();
            SerializedObject values = new SerializedObject(controller);
            values.FindProperty("developerModeEnabled").boolValue = false;
            values.FindProperty("ignoreWatchers").boolValue = true;
            values.FindProperty("allowRuntimeToggle").boolValue = true;
            values.FindProperty("toggleKey").intValue = (int)KeyCode.F1;
            values.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "LEVEL1_DEVELOPER_MODE_COMPLETE Toggle=Ctrl+Shift+F1, IgnoreWatchers=true");
        }
    }
}
#endif
