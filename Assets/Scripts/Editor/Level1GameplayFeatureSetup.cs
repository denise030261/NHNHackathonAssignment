#if UNITY_EDITOR
using NHNHackathon.Dance;
using NHNHackathon.Enemy;
using NHNHackathon.ExitSystem;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1GameplayFeatureSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Connect Camera, Doors And Patrol Rules")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int cameraZoneCount = ConfigureDanceZones();
            int watcherCount = ConfigureWatchers();
            int doorCount = ValidateDoors();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"LEVEL1_GAMEPLAY_FEATURES_CONNECTED: {cameraZoneCount} dance zones, " +
                $"{watcherCount} watchers, {doorCount} keyed doors.");
        }

        private static int ConfigureDanceZones()
        {
            DanceSyncZone[] zones = Object.FindObjectsByType<DanceSyncZone>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (DanceSyncZone zone in zones)
            {
                if (zone.GetComponent<DanceZoneCameraTrigger>() == null)
                {
                    zone.gameObject.AddComponent<DanceZoneCameraTrigger>();
                }
            }
            return zones.Length;
        }

        private static int ConfigureWatchers()
        {
            GameProgressionController progression =
                Object.FindAnyObjectByType<GameProgressionController>(FindObjectsInactive.Include);
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (EnemyController enemy in enemies)
            {
                ProgressionPatrolController controller =
                    enemy.GetComponent<ProgressionPatrolController>()
                    ?? enemy.gameObject.AddComponent<ProgressionPatrolController>();
                SerializedObject settings = new SerializedObject(controller);
                settings.FindProperty("enemyController").objectReferenceValue = enemy;
                settings.FindProperty("progressionController").objectReferenceValue = progression;
                if (settings.FindProperty("defaultPatrolRoute").objectReferenceValue == null)
                {
                    settings.FindProperty("defaultPatrolRoute").objectReferenceValue =
                        enemy.PatrolRoute;
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            return enemies.Length;
        }

        private static int ValidateDoors()
        {
            int keyedDoorCount = 0;
            ExitDoor[] doors = Object.FindObjectsByType<ExitDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ExitDoor door in doors)
            {
                SerializedObject settings = new SerializedObject(door);
                SerializedProperty keys = settings.FindProperty("requiredKeys");
                bool hasValidKey = false;
                for (int index = 0; index < keys.arraySize; index++)
                {
                    ItemDefinition key =
                        keys.GetArrayElementAtIndex(index).objectReferenceValue as ItemDefinition;
                    hasValidKey |= key != null && key.Type == ItemType.Key;
                }

                if (hasValidKey)
                {
                    keyedDoorCount++;
                }
                else if (keys.arraySize > 0)
                {
                    Debug.LogWarning($"{door.name}: required key reference is invalid.", door);
                }
            }
            return keyedDoorCount;
        }
    }
}
#endif
