#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NHNHackathon.ExitSystem;
using NHNHackathon.Items;
using NHNHackathon.Inspection;
using NHNHackathon.Lighting;
using NHNHackathon.LightSystem;
using NHNHackathon.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class DoYoungPracticeProgressionLightingSetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";
        private const string ConditionDirectory = "Assets/Data/Progression/Conditions";

        [MenuItem("Tools/NHN Hackathon/Rebuild Progression Lighting System")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("ProgressionLightingSystem");
            EnsureDirectory(ConditionDirectory);

            ProgressionCondition keepsakeCollected =
                CreateCondition("ClockworkKeepsakeCollected", "태엽 부품 획득");
            ProgressionCondition memoCollected =
                CreateCondition("FactoryMemoCollected", "공장 관리 기록 획득");
            ProgressionCondition key01Collected =
                CreateCondition("ExitKey01Collected", "출구 열쇠 1 획득");
            ProgressionCondition key02Collected =
                CreateCondition("ExitKey02Collected", "출구 열쇠 2 획득");
            ProgressionCondition key03Collected =
                CreateCondition("ExitKey03Collected", "출구 열쇠 3 획득");
            ProgressionCondition exitDoorUnlocked =
                CreateCondition("ExitDoorUnlocked", "출구 문 최초 잠금 해제");

            AssignItemCondition(
                "Assets/Data/Items/ClockworkKeepsake.asset", keepsakeCollected);
            AssignItemCondition(
                "Assets/Data/Items/FactoryMemo.asset", memoCollected);
            AssignItemCondition("Assets/Data/Items/Key_01.asset", key01Collected);
            AssignItemCondition("Assets/Data/Items/Key_02.asset", key02Collected);
            AssignItemCondition("Assets/Data/Items/Key_03.asset", key03Collected);

            GameObject root = new GameObject("ProgressionLightingSystem");
            GameProgressionController progression =
                root.AddComponent<GameProgressionController>();
            ProgressionLightingController lightingController =
                root.AddComponent<ProgressionLightingController>();

            GameObject groupObject = new GameObject("EnvironmentLights");
            groupObject.transform.SetParent(root.transform, false);
            LightingGroup environmentGroup = groupObject.AddComponent<LightingGroup>();
            AssignEnvironmentLights(environmentGroup);

            ConfigureLightingController(
                lightingController, progression, memoCollected, environmentGroup);
            ConfigureExitDoor(exitDoorUnlocked);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DoYoungPracticeScene progression lighting setup completed.");
        }

        private static ProgressionCondition CreateCondition(
            string fileName, string displayName)
        {
            string path = $"{ConditionDirectory}/{fileName}.asset";
            ProgressionCondition condition =
                AssetDatabase.LoadAssetAtPath<ProgressionCondition>(path);
            if (condition == null)
            {
                condition = ScriptableObject.CreateInstance<ProgressionCondition>();
                AssetDatabase.CreateAsset(condition, path);
            }

            SerializedObject settings = new SerializedObject(condition);
            settings.FindProperty("displayName").stringValue = displayName;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(condition);
            return condition;
        }

        private static void AssignItemCondition(
            string itemPath, ProgressionCondition condition)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            if (item == null)
            {
                return;
            }

            SerializedObject settings = new SerializedObject(item);
            settings.FindProperty("progressionCondition").objectReferenceValue = condition;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void AssignEnvironmentLights(LightingGroup group)
        {
            List<Light> environmentLights = new List<Light>();
            foreach (Light sceneLight in Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Include))
            {
                if (sceneLight == null
                    || sceneLight.GetComponentInParent<ItemPreviewRenderer>() != null
                    || sceneLight.name.Contains("Preview")
                    || sceneLight.GetComponentInParent<PlayerFlashlightController>() != null)
                {
                    continue;
                }
                environmentLights.Add(sceneLight);
            }

            SerializedObject settings = new SerializedObject(group);
            SerializedProperty lights = settings.FindProperty("lights");
            lights.arraySize = environmentLights.Count;
            for (int index = 0; index < environmentLights.Count; index++)
            {
                lights.GetArrayElementAtIndex(index).objectReferenceValue =
                    environmentLights[index];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLightingController(
            ProgressionLightingController controller,
            GameProgressionController progression,
            ProgressionCondition sampleCondition,
            LightingGroup environmentGroup)
        {
            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("progressionController").objectReferenceValue = progression;

            SerializedProperty rules = settings.FindProperty("rules");
            rules.arraySize = 1;
            SerializedProperty sampleRule = rules.GetArrayElementAtIndex(0);
            sampleRule.FindPropertyRelative("enabled").boolValue = false;
            sampleRule.FindPropertyRelative("ruleName").stringValue =
                "예시 - 관리 기록 획득 후 환경 조명 끄기 (비활성)";
            sampleRule.FindPropertyRelative("conditionMode").enumValueIndex =
                (int)ConditionMatchMode.All;

            SerializedProperty conditions =
                sampleRule.FindPropertyRelative("conditions");
            conditions.arraySize = 1;
            conditions.GetArrayElementAtIndex(0)
                .FindPropertyRelative("condition").objectReferenceValue = sampleCondition;
            conditions.GetArrayElementAtIndex(0)
                .FindPropertyRelative("mustBeCompleted").boolValue = true;

            SerializedProperty actions = sampleRule.FindPropertyRelative("actions");
            actions.arraySize = 1;
            actions.GetArrayElementAtIndex(0)
                .FindPropertyRelative("lightingGroup").objectReferenceValue = environmentGroup;
            actions.GetArrayElementAtIndex(0)
                .FindPropertyRelative("turnOn").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureExitDoor(ProgressionCondition condition)
        {
            ExitDoor exitDoor = Object.FindAnyObjectByType<ExitDoor>();
            if (exitDoor == null)
            {
                return;
            }

            SerializedObject settings = new SerializedObject(exitDoor);
            settings.FindProperty("unlockedCondition").objectReferenceValue = condition;
            settings.ApplyModifiedPropertiesWithoutUndo();
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

            string parent = Path.GetDirectoryName(directory)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureDirectory(parent);
            }
            AssetDatabase.CreateFolder(parent, Path.GetFileName(directory));
        }

    }
}
#endif
