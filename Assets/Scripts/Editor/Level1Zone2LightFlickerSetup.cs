#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.ExitSystem;
using NHNHackathon.Lighting;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1Zone2LightFlickerSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Connect Zone1_2 Slam To Zone2 Light")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject zone12 = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Zone1_2")
                ?? throw new System.InvalidOperationException("Zone1_2 was not found.");
            DoorPassageAutoSlam slam = zone12.GetComponentInChildren<DoorPassageAutoSlam>(true)
                ?? throw new System.InvalidOperationException("Zone1_2 auto slam trigger was not found.");

            GameObject zone2 = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Zone2")
                ?? throw new System.InvalidOperationException("Zone2 was not found.");
            Transform lightsRoot = zone2.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "Lights")
                ?? throw new System.InvalidOperationException("Zone2/Lights was not found.");
            Transform lightTransform = lightsRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "Point Light")
                ?? throw new System.InvalidOperationException("Zone2/Lights/Point Light was not found.");
            Light targetLight = lightTransform.GetComponent<Light>()
                ?? throw new System.InvalidOperationException("Point Light has no Light component.");
            LightFlickerEffect flicker = lightTransform.GetComponent<LightFlickerEffect>();
            if (flicker == null)
            {
                flicker = lightTransform.gameObject.AddComponent<LightFlickerEffect>();
            }

            SerializedObject flickerValues = new(flicker);
            flickerValues.FindProperty("targetLight").objectReferenceValue = targetLight;
            flickerValues.FindProperty("flickerCount").intValue = 5;
            flickerValues.FindProperty("minimumInterval").floatValue = 0.06f;
            flickerValues.FindProperty("maximumInterval").floatValue = 0.16f;
            flickerValues.ApplyModifiedPropertiesWithoutUndo();

            slam.OnSlammed.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(slam.OnSlammed, flicker.Play);
            EditorUtility.SetDirty(slam);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_ZONE2_LIGHT_FLICKER_COMPLETE");
        }
    }
}
#endif
