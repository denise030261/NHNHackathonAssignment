#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Lighting;
using NHNHackathon.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1Key01LightOffSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string ConditionPath =
            "Assets/Data/Progression/Conditions/ExitKey01Collected.asset";

        [MenuItem("Tools/NHN Hackathon/Level1/Connect Key01 To Zone2 Light Off")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject zone2 = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Zone2")
                ?? throw new System.InvalidOperationException("Zone2 was not found.");
            Transform lights = zone2.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "Lights")
                ?? throw new System.InvalidOperationException("Zone2/Lights was not found.");
            Transform pointLight = lights.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "Point Light")
                ?? throw new System.InvalidOperationException("Zone2/Lights/Point Light was not found.");

            Light light = pointLight.GetComponent<Light>()
                ?? throw new System.InvalidOperationException("Point Light has no Light component.");
            LightFlickerEffect flicker = pointLight.GetComponent<LightFlickerEffect>();
            ProgressionLightSwitch lightSwitch = pointLight.GetComponent<ProgressionLightSwitch>();
            if (lightSwitch == null)
            {
                lightSwitch = pointLight.gameObject.AddComponent<ProgressionLightSwitch>();
            }

            SerializedObject values = new(lightSwitch);
            values.FindProperty("condition").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ProgressionCondition>(ConditionPath);
            values.FindProperty("stateWhenCompleted").boolValue = false;
            values.FindProperty("targetLight").objectReferenceValue = light;
            values.FindProperty("flickerEffect").objectReferenceValue = flicker;
            values.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_KEY01_LIGHT_OFF_COMPLETE");
        }
    }
}
#endif
