#if UNITY_EDITOR
using NHNHackathon.Dance;
using NHNHackathon.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1TutorialDanceRewardSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string BaseKeyPrefabPath = "Assets/Prefabs/Interactables/Key.prefab";
        private const string TutorialKeyPrefabPath =
            "Assets/Prefabs/Interactables/Key_Tutorial.prefab";
        private const string TutorialKeyDataPath =
            "Assets/Data/Items/Key/Key_Tutorial.asset";

        [MenuItem("Tools/NHN Hackathon/Level1/Setup Tutorial Dance Key Reward")]
        public static void Build()
        {
            GameObject rewardPrefab = CreateTutorialKeyPrefab();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform tutorialMechanics = GameObject.Find("Zone2")?.transform.Find("DancinZone");
            DanceSyncZone zone = tutorialMechanics != null
                ? tutorialMechanics.GetComponentInChildren<DanceSyncZone>(true)
                : null;
            if (zone == null)
            {
                throw new System.InvalidOperationException(
                    "Tutorial DanceSyncZone was not found below Zone2/DancinZone.");
            }

            TutorialDanceReward reward = zone.GetComponent<TutorialDanceReward>()
                ?? zone.gameObject.AddComponent<TutorialDanceReward>();
            Transform dropPoint = GetOrCreateDropPoint(zone);

            SerializedObject settings = new SerializedObject(reward);
            settings.FindProperty("danceZone").objectReferenceValue = zone;
            settings.FindProperty("syncJudge").objectReferenceValue =
                zone.GetComponent<DanceSyncJudge>();
            settings.FindProperty("rewardPrefab").objectReferenceValue = rewardPrefab;
            settings.FindProperty("dropPoint").objectReferenceValue = dropPoint;
            settings.FindProperty("requiredSuccessDuration").floatValue = 3f;
            settings.FindProperty("dropHeight").floatValue = 2.5f;
            settings.FindProperty("dropDuration").floatValue = 0.9f;
            settings.FindProperty("rotation").vector3Value = new Vector3(0f, 540f, 0f);
            settings.FindProperty("rotationDuration").floatValue = 0.9f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"TUTORIAL_DANCE_REWARD_CONNECTED: {GetHierarchyPath(zone.transform)}, " +
                $"drop={dropPoint.position}, reward={TutorialKeyPrefabPath}");
        }

        private static GameObject CreateTutorialKeyPrefab()
        {
            ItemDefinition definition =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(TutorialKeyDataPath);
            if (definition == null)
            {
                throw new System.InvalidOperationException(
                    $"Missing tutorial key data: {TutorialKeyDataPath}");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TutorialKeyPrefabPath) == null
                && !AssetDatabase.CopyAsset(BaseKeyPrefabPath, TutorialKeyPrefabPath))
            {
                throw new System.InvalidOperationException(
                    $"Could not create {TutorialKeyPrefabPath}.");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(TutorialKeyPrefabPath);
            try
            {
                root.name = "Key_Tutorial";
                KeyCollectible collectible = root.GetComponent<KeyCollectible>();
                if (collectible == null)
                {
                    throw new System.InvalidOperationException(
                        "The base key prefab requires KeyCollectible.");
                }

                SerializedObject settings = new SerializedObject(collectible);
                settings.FindProperty("itemDefinition").objectReferenceValue = definition;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, TutorialKeyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(TutorialKeyPrefabPath);
        }

        private static Transform GetOrCreateDropPoint(DanceSyncZone zone)
        {
            Transform existing = zone.transform.Find("TutorialKeyDropPoint");
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("TutorialKeyDropPoint");
            root.transform.SetParent(zone.transform, true);
            Collider zoneCollider = zone.GetComponent<Collider>();
            Vector3 origin = zoneCollider != null
                ? zoneCollider.bounds.center + Vector3.up * 5f
                : zone.transform.position + Vector3.up * 5f;
            Vector3 landing = zone.transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(
                    origin, Vector3.down, out RaycastHit hit, 20f,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                landing = hit.point + Vector3.up * 0.35f;
            }
            root.transform.position = landing;
            return root.transform;
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }
            return path;
        }
    }
}
#endif
