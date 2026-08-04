#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Lighting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1WebHorrorLightingSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Apply Web Horror Environment")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindSceneObject("EnvironmentLighting");
            if (root == null)
            {
                root = new GameObject("EnvironmentLighting");
            }

            WebHorrorEnvironmentController controller =
                root.GetComponent<WebHorrorEnvironmentController>()
                ?? root.AddComponent<WebHorrorEnvironmentController>();
            SerializedObject settings = new(controller);
            settings.FindProperty("ambientColor").colorValue =
                new Color(0.003f, 0.004f, 0.008f, 1f);
            settings.FindProperty("ambientIntensity").floatValue = 0.04f;
            settings.FindProperty("reflectionIntensity").floatValue = 0.08f;
            settings.FindProperty("reflectionBounces").intValue = 1;
            settings.FindProperty("forceSolidCameraBackground").boolValue = true;
            settings.FindProperty("cameraBackgroundColor").colorValue = Color.black;
            settings.FindProperty("enableFog").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.003f, 0.004f, 0.008f, 1f);
            RenderSettings.ambientIntensity = 0.04f;
            RenderSettings.reflectionIntensity = 0.08f;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.fog = false;

            foreach (Camera camera in Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.cameraType == CameraType.Game)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.black;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "LEVEL1_WEB_HORROR_LIGHTING_COMPLETE: dark ambient applied; local lights preserved.");
        }

        private static GameObject FindSceneObject(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(value => value.scene.IsValid() && value.name == name);
        }
    }
}
#endif
