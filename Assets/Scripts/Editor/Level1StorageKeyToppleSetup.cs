#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Cinematics;
using NHNHackathon.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1StorageKeyToppleSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string ConditionPath =
            "Assets/Data/Progression/Conditions/StorageKeyCollected.asset";
        private const string StorageKeyPath = "Assets/Data/Items/Key/Key_Storage.asset";

        [MenuItem("Tools/NHN Hackathon/Level1/Connect Storage Key Topple Sequence")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject zone4 = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Zone4")
                ?? throw new System.InvalidOperationException("Zone4 was not found.");
            Transform toies = zone4.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "Toies")
                ?? throw new System.InvalidOperationException("Zone4/Toies was not found.");
            Transform[] models = toies.Cast<Transform>().ToArray();
            if (models.Length == 0)
                throw new System.InvalidOperationException("Zone4/Toies has no child models.");

            ProgressionToppleSequence sequence =
                toies.GetComponent<ProgressionToppleSequence>();
            if (sequence == null)
            {
                sequence = toies.gameObject.AddComponent<ProgressionToppleSequence>();
            }

            Vector3[] directions =
            {
                new(88f, 0f, 8f),
                new(-84f, 0f, -10f),
                new(12f, 0f, 86f),
                new(-8f, 0f, -88f)
            };
            SerializedObject values = new(sequence);
            values.FindProperty("condition").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ProgressionCondition>(ConditionPath);
            values.FindProperty("triggerItem").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<NHNHackathon.Items.ItemDefinition>(StorageKeyPath);
            values.FindProperty("waitForInspectionClose").boolValue = true;
            values.FindProperty("oneShot").boolValue = true;
            SerializedProperty targets = values.FindProperty("targets");
            targets.arraySize = models.Length;
            for (int index = 0; index < models.Length; index++)
            {
                SerializedProperty target = targets.GetArrayElementAtIndex(index);
                target.FindPropertyRelative("target").objectReferenceValue = models[index];
                target.FindPropertyRelative("fallEulerAngles").vector3Value =
                    directions[index % directions.Length];
                target.FindPropertyRelative("delay").floatValue = index * 0.12f;
                target.FindPropertyRelative("duration").floatValue = 0.55f + index * 0.04f;
            }
            values.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"LEVEL1_STORAGE_KEY_TOPPLE_COMPLETE: {models.Length} models connected.");
        }
    }
}
#endif
