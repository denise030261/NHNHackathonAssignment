#if UNITY_EDITOR
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.ExitSystem;
using NHNHackathon.Game;
using NHNHackathon.Input;
using NHNHackathon.Items;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class DoYoungPracticeExitSetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";
        private const string KeyPrefabPath = "Assets/Prefabs/Interactables/Key.prefab";
        private const string DoorPrefabPath = "Assets/Prefabs/Interactables/ExitDoor.prefab";
        private const string ZonePrefabPath = "Assets/Prefabs/Interactables/ExitSuccessZone.prefab";

        [MenuItem("Tools/NHN Hackathon/Rebuild DoYoung Exit Mechanic")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("ExitMechanic");

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new System.InvalidOperationException(
                    "DoYoungPracticeScene requires a Player object.");
            }

            PlayerKeyInventory inventory =
                player.GetComponent<PlayerKeyInventory>() ?? player.AddComponent<PlayerKeyInventory>();
            SerializedObject inventorySettings = new SerializedObject(inventory);
            inventorySettings.FindProperty("requiredKeyCount").intValue = 3;
            inventorySettings.ApplyModifiedPropertiesWithoutUndo();

            GameObject mechanicRoot = new GameObject("ExitMechanic");
            GameSuccessController successController =
                CreateSuccessController(mechanicRoot.transform, player);

            Material keyMaterial = CreateMaterial(
                "Assets/Art/Materials/KeyPrototype.mat",
                new Color(1f, 0.65f, 0.08f));
            CreateKey("Key_01", new Vector3(-8f, 0.7f, 7.5f), keyMaterial, mechanicRoot.transform);
            CreateKey("Key_02", new Vector3(8f, 0.7f, 3f), keyMaterial, mechanicRoot.transform);
            CreateKey("Key_03", new Vector3(-8f, 0.7f, -1f), keyMaterial, mechanicRoot.transform);

            ExitDoor door = CreateDoor(inventory, mechanicRoot.transform);
            CreateSuccessZone(door, successController, mechanicRoot.transform);

            EnsureDirectory("Assets/Prefabs/Interactables");
            GameObject firstKey = GameObject.Find("Key_01");
            if (firstKey != null)
            {
                PrefabUtility.SaveAsPrefabAsset(firstKey, KeyPrefabPath);
            }
            PrefabUtility.SaveAsPrefabAsset(door.gameObject, DoorPrefabPath);
            GameObject successZone = GameObject.Find("ExitSuccessZone");
            if (successZone != null)
            {
                PrefabUtility.SaveAsPrefabAsset(successZone, ZonePrefabPath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DoYoungPracticeScene exit mechanic setup completed.");
        }

        private static GameSuccessController CreateSuccessController(
            Transform parent, GameObject player)
        {
            GameObject systemObject = new GameObject("GameSuccessSystem");
            systemObject.transform.SetParent(parent);
            GameSuccessController controller = systemObject.AddComponent<GameSuccessController>();
            Behaviour[] controls =
            {
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<PlayerCameraController>(),
                player.GetComponent<PlayerDanceInput>(),
                player.GetComponent<PlayerCursorController>(),
                player.GetComponentInChildren<PlayerFlashlightController>(true)
            };

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("gameOverController").objectReferenceValue =
                Object.FindAnyObjectByType<GameOverController>();
            SerializedProperty controlsProperty = settings.FindProperty("playerControls");
            controlsProperty.arraySize = controls.Length;
            for (int index = 0; index < controls.Length; index++)
            {
                controlsProperty.GetArrayElementAtIndex(index).objectReferenceValue = controls[index];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void CreateKey(
            string keyId, Vector3 position, Material material, Transform parent)
        {
            GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cube);
            key.name = keyId;
            key.transform.SetParent(parent);
            key.transform.position = position;
            key.transform.localScale = new Vector3(0.35f, 0.15f, 0.65f);
            key.GetComponent<Renderer>().sharedMaterial = material;
            key.GetComponent<BoxCollider>().isTrigger = true;
            KeyCollectible collectible = key.AddComponent<KeyCollectible>();
            key.AddComponent<KeyCollectibleVisual>();

            SerializedObject settings = new SerializedObject(collectible);
            settings.FindProperty("keyId").stringValue = keyId;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ExitDoor CreateDoor(PlayerKeyInventory inventory, Transform parent)
        {
            Material doorMaterial = CreateMaterial(
                "Assets/Art/Materials/ExitDoorPrototype.mat",
                new Color(0.12f, 0.14f, 0.17f));

            GameObject root = new GameObject("ExitDoor");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(-2f, 0f, 9f);
            BoxCollider unlockTrigger = root.AddComponent<BoxCollider>();
            unlockTrigger.isTrigger = true;
            unlockTrigger.center = new Vector3(2f, 1.5f, -1f);
            unlockTrigger.size = new Vector3(4.4f, 3f, 2f);
            ExitDoor door = root.AddComponent<ExitDoor>();

            GameObject pivot = new GameObject("DoorPivot");
            pivot.transform.SetParent(root.transform, false);

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(pivot.transform, false);
            panel.transform.localPosition = new Vector3(2f, 1.5f, 0f);
            panel.transform.localScale = new Vector3(4f, 3f, 0.25f);
            panel.GetComponent<Renderer>().sharedMaterial = doorMaterial;
            BoxCollider blockingCollider = panel.GetComponent<BoxCollider>();

            CreateFramePart(
                "LeftFrame", new Vector3(-0.2f, 1.65f, 0f),
                new Vector3(0.4f, 3.3f, 0.5f), doorMaterial, root.transform);
            CreateFramePart(
                "RightFrame", new Vector3(4.2f, 1.65f, 0f),
                new Vector3(0.4f, 3.3f, 0.5f), doorMaterial, root.transform);
            CreateFramePart(
                "TopFrame", new Vector3(2f, 3.2f, 0f),
                new Vector3(4.8f, 0.4f, 0.5f), doorMaterial, root.transform);

            SerializedObject settings = new SerializedObject(door);
            settings.FindProperty("requiredKeys").intValue = 3;
            settings.FindProperty("playerInventory").objectReferenceValue = inventory;
            settings.FindProperty("doorPanel").objectReferenceValue = pivot.transform;
            settings.FindProperty("blockingCollider").objectReferenceValue = blockingCollider;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        private static void CreateFramePart(
            string name, Vector3 localPosition, Vector3 localScale,
            Material material, Transform parent)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateSuccessZone(
            ExitDoor door, GameSuccessController successController, Transform parent)
        {
            GameObject zone = new GameObject("ExitSuccessZone");
            zone.transform.SetParent(parent);
            zone.transform.position = new Vector3(0f, 1.2f, 10.4f);
            BoxCollider collider = zone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 2.4f, 1.2f);
            ExitSuccessZone successZone = zone.AddComponent<ExitSuccessZone>();

            SerializedObject settings = new SerializedObject(successZone);
            settings.FindProperty("exitDoor").objectReferenceValue = door;
            settings.FindProperty("successController").objectReferenceValue = successController;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            EnsureDirectory("Assets/Art/Materials");
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

        private static void EnsureDirectory(string directory)
        {
            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(directory)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureDirectory(parent);
            }
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(directory));
        }
    }
}
#endif
