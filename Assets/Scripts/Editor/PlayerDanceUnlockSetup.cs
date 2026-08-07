#if UNITY_EDITOR
using NHNHackathon.Dance;
using NHNHackathon.Interaction;
using UnityEditor;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class PlayerDanceUnlockSetup
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player.prefab";

        [MenuItem("NHN Hackathon/Setup/Player Dance Unlock")]
        public static void Setup()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerDanceUnlockController unlockController =
                    root.GetComponent<PlayerDanceUnlockController>();
                if (unlockController == null)
                {
                    unlockController = root.AddComponent<PlayerDanceUnlockController>();
                }

                PlayerInteractor interactor = root.GetComponent<PlayerInteractor>();
                SerializedObject unlockSerialized = new SerializedObject(unlockController);
                unlockSerialized.FindProperty("playerInteractor").objectReferenceValue = interactor;

                SerializedProperty rules = unlockSerialized.FindProperty("unlockRules");
                if (rules.arraySize == 0)
                {
                    rules.arraySize = 4;
                    for (int index = 0; index < rules.arraySize; index++)
                    {
                        SerializedProperty rule = rules.GetArrayElementAtIndex(index);
                        rule.FindPropertyRelative("danceId").intValue = index + 1;
                    }
                }
                unlockSerialized.ApplyModifiedPropertiesWithoutUndo();

                PlayerDanceInput danceInput = root.GetComponent<PlayerDanceInput>();
                SerializedObject inputSerialized = new SerializedObject(danceInput);
                inputSerialized.FindProperty("unlockController").objectReferenceValue = unlockController;
                inputSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Player dance unlock setup completed. Assign a paper asset to each Unlock Rules entry.");
        }

        public static void SetupFromCommandLine()
        {
            Setup();
        }
    }
}
#endif
