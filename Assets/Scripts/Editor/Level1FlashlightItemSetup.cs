#if UNITY_EDITOR
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1FlashlightItemSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player.prefab";
        private const string ItemPath = "Assets/Data/Items/Flashlight.asset";
        private const string PrefabPath = "Assets/Prefabs/Interactables/FlashlightPickup.prefab";
        private const string MaterialPath = "Assets/Art/Materials/FlashlightPickup.mat";

        [MenuItem("Tools/NHN Hackathon/Level1/Setup Flashlight Item Requirement")]
        public static void Build()
        {
            ItemDefinition item = CreateItemDefinition();
            GameObject pickupPrefab = CreatePickupPrefab(item);
            ConfigureItemPreview(item, pickupPrefab);
            ConfigurePlayerPrefab(item);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayerItemInventory inventory =
                Object.FindAnyObjectByType<PlayerItemInventory>(FindObjectsInactive.Include);
            if (inventory == null)
            {
                throw new System.InvalidOperationException("Level1 requires PlayerItemInventory.");
            }

            ConfigureFlashlight(
                inventory.GetComponentInChildren<PlayerFlashlightController>(true), item);
            PlacePickup(inventory.transform, pickupPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_FLASHLIGHT_ITEM_CONNECTED: flashlight starts off and requires Flashlight.asset.");
        }

        private static ItemDefinition CreateItemDefinition()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, ItemPath);
            }

            SerializedObject settings = new SerializedObject(item);
            settings.FindProperty("itemId").stringValue = "Flashlight";
            settings.FindProperty("displayName").stringValue = "손전등";
            settings.FindProperty("itemType").enumValueIndex = (int)ItemType.General;
            settings.FindProperty("description").stringValue =
                "어둠 속에서 길과 인형의 춤을 확인할 수 있는 손전등이다.";
            settings.FindProperty("inspectOnPickup").boolValue = true;
            settings.FindProperty("previewEulerAngles").vector3Value = new Vector3(15f, 30f, 0f);
            settings.FindProperty("previewScale").floatValue = 1.2f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static GameObject CreatePickupPrefab(ItemDefinition item)
        {
            Material material = CreateMaterial();
            GameObject root = new GameObject("FlashlightPickup");
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.45f, 0.35f, 1.1f);
            InspectableItem collectible = root.AddComponent<InspectableItem>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.18f, 0.42f, 0.18f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.48f);
            head.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            head.transform.localScale = new Vector3(0.3f, 0.12f, 0.3f);
            head.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            SerializedObject settings = new SerializedObject(collectible);
            settings.FindProperty("item").objectReferenceValue = item;
            settings.FindProperty("interactionPrompt").stringValue = "손전등 획득";
            settings.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureItemPreview(ItemDefinition item, GameObject pickupPrefab)
        {
            SerializedObject settings = new SerializedObject(item);
            settings.FindProperty("previewPrefab").objectReferenceValue = pickupPrefab;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void ConfigurePlayerPrefab(ItemDefinition item)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ConfigureFlashlight(
                    root.GetComponentInChildren<PlayerFlashlightController>(true), item);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFlashlight(
            PlayerFlashlightController controller, ItemDefinition item)
        {
            if (controller == null)
            {
                throw new System.InvalidOperationException(
                    "PlayerFlashlightController was not found.");
            }

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("startEnabled").boolValue = false;
            settings.FindProperty("requiredFlashlightItem").objectReferenceValue = item;
            settings.FindProperty("playerInventory").objectReferenceValue =
                controller.GetComponentInParent<PlayerItemInventory>();
            settings.FindProperty("playerInteractor").objectReferenceValue =
                controller.GetComponentInParent<PlayerInteractor>();
            settings.ApplyModifiedPropertiesWithoutUndo();

            Light flashlight = controller.GetComponent<Light>();
            if (flashlight != null)
            {
                flashlight.enabled = false;
            }
        }

        private static void PlacePickup(Transform player, GameObject prefab)
        {
            GameObject pickup = GameObject.Find("FlashlightPickup");
            if (pickup == null)
            {
                pickup = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            }
            if (pickup == null)
            {
                throw new System.InvalidOperationException("Could not instantiate flashlight pickup.");
            }

            Transform zone2 = GameObject.Find("Zone2")?.transform;
            pickup.transform.SetParent(zone2, true);
            Vector3 position = player.position + player.forward * 1.5f;
            Vector3 origin = position + Vector3.up * 5f;
            if (Physics.Raycast(
                    origin, Vector3.down, out RaycastHit hit, 20f,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y + 0.35f;
            }
            else
            {
                position.y = player.position.y + 0.35f;
            }
            pickup.transform.SetPositionAndRotation(position, player.rotation);
        }

        private static Material CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                color = new Color(0.12f, 0.13f, 0.15f)
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
#endif
