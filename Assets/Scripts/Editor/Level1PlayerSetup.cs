#if UNITY_EDITOR
using NHNHackathon.Characters;
using NHNHackathon.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1PlayerSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string PrefabPath = "Assets/Prefabs/Characters/Player.prefab";
        [MenuItem("Tools/NHN Hackathon/Rebuild Level1 Player Setup")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("Player");
            DeleteNamedObject("PlayerMovementEnvironment");

            Camera existingCamera = Object.FindFirstObjectByType<Camera>();
            if (existingCamera != null)
            {
                Object.DestroyImmediate(existingCamera.gameObject);
            }

            GameObject player = CreatePlayer();
            CreateEnvironment();
            ConfigureLighting();

            EnsureDirectory("Assets/Prefabs/Characters");
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Level1 player movement setup completed.");
        }

        private static GameObject CreatePlayer()
        {
            GameObject root = new GameObject("Player");
            root.transform.position = new Vector3(0f, 1f, -6f);
            SetLayerRecursively(root, 2);

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.center = Vector3.zero;
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.skinWidth = 0.05f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            SetLayerRecursively(visual, 2);

            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(
                "Assets/Art/Materials/PlayerPrototype.mat", new Color(0.65f, 0.68f, 0.72f));

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform, false);
            Camera playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();
            SetLayerRecursively(cameraObject, 2);

            PlayerCameraController cameraController = root.AddComponent<PlayerCameraController>();
            PlayerMovement movement = root.AddComponent<PlayerMovement>();
            root.AddComponent<PlayerCursorController>();

            SerializedObject cameraSettings = new SerializedObject(cameraController);
            cameraSettings.FindProperty("perspective").enumValueIndex = (int)CameraPerspective.ThirdPerson;
            cameraSettings.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            cameraSettings.FindProperty("collisionMask").intValue = ~(1 << 2);
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject movementSettings = new SerializedObject(movement);
            movementSettings.FindProperty("movementCamera").objectReferenceValue = playerCamera.transform;
            movementSettings.FindProperty("cameraController").objectReferenceValue = cameraController;
            movementSettings.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void CreateEnvironment()
        {
            GameObject environment = new GameObject("PlayerMovementEnvironment");
            CreateCube("Floor", new Vector3(0f, -0.25f, 0f), new Vector3(22f, 0.5f, 24f), environment.transform);
            CreateCube("BackWall", new Vector3(0f, 2f, 11.5f), new Vector3(22f, 4.5f, 0.5f), environment.transform);
            CreateCube("LeftWall", new Vector3(-10.75f, 2f, 0f), new Vector3(0.5f, 4.5f, 24f), environment.transform);
            CreateCube("RightWall", new Vector3(10.75f, 2f, 0f), new Vector3(0.5f, 4.5f, 24f), environment.transform);
            CreateCube("CameraCollisionWall", new Vector3(0f, 1.5f, -2f), new Vector3(7f, 3f, 0.4f), environment.transform);
            CreateCube("NarrowPassageLeft", new Vector3(-2.25f, 1.5f, 5f), new Vector3(0.5f, 3f, 7f), environment.transform);
            CreateCube("NarrowPassageRight", new Vector3(2.25f, 1.5f, 5f), new Vector3(0.5f, 3f, 7f), environment.transform);
        }

        private static void CreateCube(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                "Assets/Art/Materials/PrototypeEnvironment.mat", new Color(0.24f, 0.27f, 0.3f));
        }

        private static void ConfigureLighting()
        {
            Light light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            light.intensity = 1.1f;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            EnsureDirectory(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void DeleteNamedObject(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void EnsureDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(directory)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(directory);
            EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
