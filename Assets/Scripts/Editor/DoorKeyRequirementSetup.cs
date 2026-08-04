#if UNITY_EDITOR
using NHNHackathon.ExitSystem;
using NHNHackathon.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class DoorKeyRequirementSetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";
        private const string DoorPrefabPath = "Assets/Prefabs/Interactables/ExitDoor.prefab";
        private const string KeyPrefabPath = "Assets/Prefabs/Interactables/Key.prefab";

        [MenuItem("Tools/NHN Hackathon/Doors/Apply Specific Key Requirements")]
        public static void Build()
        {
            ItemDefinition[] exitKeys = LoadExitKeys();
            ConfigureDoorPrefab(exitKeys);
            ConfigureKeyPrefab(exitKeys[0]);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayerItemInventory inventory = Object.FindAnyObjectByType<PlayerItemInventory>();
            if (inventory == null)
            {
                throw new System.InvalidOperationException(
                    "DoYoungPracticeScene requires PlayerItemInventory.");
            }

            ExitDoor[] doors = Object.FindObjectsByType<ExitDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ExitDoor door in doors)
            {
                ConfigureDoor(door, inventory, exitKeys);
            }

            for (int index = 0; index < exitKeys.Length; index++)
            {
                GameObject keyObject = GameObject.Find($"Key_0{index + 1}");
                if (keyObject == null || !keyObject.TryGetComponent(out KeyCollectible collectible))
                {
                    Debug.LogWarning($"Key_0{index + 1} was not found in {ScenePath}.");
                    continue;
                }
                ConfigureKey(collectible, exitKeys[index]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Specific key requirements applied to doors and key collectibles.");
        }

        private static ItemDefinition[] LoadExitKeys()
        {
            ItemDefinition[] keys = new ItemDefinition[3];
            for (int index = 0; index < keys.Length; index++)
            {
                string path = $"Assets/Data/Items/Key_0{index + 1}.asset";
                keys[index] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (keys[index] == null)
                {
                    throw new System.InvalidOperationException($"Missing key data: {path}");
                }
            }
            return keys;
        }

        private static void ConfigureDoor(
            ExitDoor door, PlayerItemInventory inventory, ItemDefinition[] keys)
        {
            SerializedObject settings = new SerializedObject(door);
            SerializedProperty requiredKeys = settings.FindProperty("requiredKeys");
            requiredKeys.arraySize = keys.Length;
            for (int index = 0; index < keys.Length; index++)
            {
                requiredKeys.GetArrayElementAtIndex(index).objectReferenceValue = keys[index];
            }
            settings.FindProperty("consumeKeysOnUnlock").boolValue = false;
            settings.FindProperty("playerInventory").objectReferenceValue = inventory;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDoorPrefab(ItemDefinition[] keys)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(DoorPrefabPath);
            try
            {
                ExitDoor door = root.GetComponent<ExitDoor>();
                SerializedObject settings = new SerializedObject(door);
                SerializedProperty requiredKeys = settings.FindProperty("requiredKeys");
                requiredKeys.arraySize = keys.Length;
                for (int index = 0; index < keys.Length; index++)
                {
                    requiredKeys.GetArrayElementAtIndex(index).objectReferenceValue = keys[index];
                }
                settings.FindProperty("consumeKeysOnUnlock").boolValue = false;
                settings.FindProperty("playerInventory").objectReferenceValue = null;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, DoorPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureKeyPrefab(ItemDefinition definition)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(KeyPrefabPath);
            try
            {
                KeyCollectible collectible = root.GetComponent<KeyCollectible>();
                ConfigureKey(collectible, definition);
                PrefabUtility.SaveAsPrefabAsset(root, KeyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureKey(KeyCollectible collectible, ItemDefinition definition)
        {
            SerializedObject settings = new SerializedObject(collectible);
            settings.FindProperty("itemDefinition").objectReferenceValue = definition;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
