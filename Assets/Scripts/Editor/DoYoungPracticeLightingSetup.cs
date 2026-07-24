#if UNITY_EDITOR
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class DoYoungPracticeLightingSetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";

        [MenuItem("Tools/NHN Hackathon/Rebuild DoYoung Indoor Lighting")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ConfigureIndoorEnvironment();

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new System.InvalidOperationException(
                    "DoYoungPracticeScene requires a Player object.");
            }

            ConfigurePlayerLights(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DoYoungPracticeScene indoor lighting setup completed.");
        }

        private static void ConfigureIndoorEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.004f, 0.005f, 0.008f);
            RenderSettings.ambientIntensity = 0.05f;
            RenderSettings.reflectionIntensity = 0.05f;
            RenderSettings.fog = false;

            GameObject directionalObject = GameObject.Find("Directional Light");
            if (directionalObject != null
                && directionalObject.TryGetComponent(out Light directionalLight))
            {
                directionalLight.intensity = 0.015f;
                directionalLight.color = new Color(0.35f, 0.4f, 0.5f);
                directionalLight.shadows = LightShadows.None;
            }

            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
            }
        }

        private static void ConfigurePlayerLights(GameObject player)
        {
            Transform existingProximity = player.transform.Find("ProximityLight");
            if (existingProximity != null)
            {
                Object.DestroyImmediate(existingProximity.gameObject);
            }

            GameObject proximityObject = new GameObject("ProximityLight");
            proximityObject.transform.SetParent(player.transform, false);
            proximityObject.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            Light proximityLight = proximityObject.AddComponent<Light>();
            proximityLight.type = LightType.Point;
            proximityLight.range = 2.2f;
            proximityLight.intensity = 0.18f;
            proximityLight.color = new Color(0.45f, 0.52f, 0.65f);
            proximityLight.shadows = LightShadows.None;
            proximityLight.bounceIntensity = 0f;

            LightStimulusSource stimulus =
                player.GetComponentInChildren<LightStimulusSource>(true);
            if (stimulus == null || !stimulus.TryGetComponent(out Light flashlight))
            {
                throw new System.InvalidOperationException(
                    "Player requires a flashlight LightStimulusSource.");
            }

            flashlight.type = LightType.Spot;
            flashlight.range = 12f;
            flashlight.spotAngle = 52f;
            flashlight.intensity = 8f;
            flashlight.color = new Color(0.82f, 0.88f, 1f);
            flashlight.shadows = LightShadows.Hard;
            flashlight.shadowStrength = 0.8f;
            flashlight.bounceIntensity = 0f;
        }
    }
}
#endif
