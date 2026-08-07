#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1UniquePaperItemsSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string SourceItemPath = "Assets/Data/Items/FactoryMemo.asset";
        private const string SourceConditionPath =
            "Assets/Data/Progression/Conditions/FactoryMemoCollected.asset";

        private static readonly string[] TargetZones = { "Zone3", "Zone4", "Zone5", "Zone7" };

        [MenuItem("NHN Hackathon/Setup/Level1 Unique Paper Items")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ItemDefinition sourceItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(SourceItemPath);
            if (sourceItem == null)
            {
                throw new System.InvalidOperationException("FactoryMemo ItemDefinition was not found.");
            }

            foreach (string zoneName in TargetZones)
            {
                ItemDefinition item = CreateOrUpdateItem(zoneName);
                GameObject zone = scene.GetRootGameObjects().FirstOrDefault(root => root.name == zoneName)
                    ?? throw new System.InvalidOperationException($"{zoneName} was not found.");
                InspectableItem paper = zone.GetComponentsInChildren<InspectableItem>(true)
                    .FirstOrDefault(value =>
                    {
                        SerializedObject check = new SerializedObject(value);
                        ItemDefinition current = check.FindProperty("item").objectReferenceValue as ItemDefinition;
                        return current == sourceItem || value.name.Contains("FactoryMemo");
                    })
                    ?? throw new System.InvalidOperationException($"Paper item in {zoneName} was not found.");

                SerializedObject paperValues = new SerializedObject(paper);
                paperValues.FindProperty("item").objectReferenceValue = item;
                paperValues.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(paper);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_UNIQUE_PAPERS_COMPLETE Zone1, Zone3, Zone4, Zone5, Zone7 use unique ItemIds.");
        }

        private static ItemDefinition CreateOrUpdateItem(string zoneName)
        {
            string itemPath = $"Assets/Data/Items/FactoryMemo_{zoneName}.asset";
            string conditionPath =
                $"Assets/Data/Progression/Conditions/FactoryMemoCollected_{zoneName}.asset";
            CopyIfMissing(SourceConditionPath, conditionPath);
            CopyIfMissing(SourceItemPath, itemPath);

            ProgressionCondition condition =
                AssetDatabase.LoadAssetAtPath<ProgressionCondition>(conditionPath);
            SerializedObject conditionValues = new SerializedObject(condition);
            conditionValues.FindProperty("displayName").stringValue = $"{zoneName} 기록 획득";
            conditionValues.FindProperty("description").stringValue =
                $"{zoneName}에 배치된 기록지를 획득했다.";
            conditionValues.ApplyModifiedPropertiesWithoutUndo();

            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            SerializedObject itemValues = new SerializedObject(item);
            itemValues.FindProperty("itemId").stringValue = $"Factory_Memo_{zoneName}";
            itemValues.FindProperty("displayName").stringValue = $"공장 관리 기록 ({zoneName})";
            itemValues.FindProperty("progressionCondition").objectReferenceValue = condition;
            itemValues.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(condition);
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destination) == null
                && !AssetDatabase.CopyAsset(source, destination))
            {
                throw new System.InvalidOperationException($"Could not create {destination}.");
            }
        }
    }
}
#endif
