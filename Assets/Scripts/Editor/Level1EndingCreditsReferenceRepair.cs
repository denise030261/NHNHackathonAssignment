#if UNITY_EDITOR
using System;
using System.Linq;
using NHNHackathon.ExitSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    /// <summary>
    /// Connects scene-only references between the ending system prefab and UI prefab.
    /// Cross-prefab references cannot be stored in either prefab asset, so they belong
    /// to the Level1 prefab instances.
    /// </summary>
    public static class Level1EndingCreditsReferenceRepair
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [InitializeOnLoadMethod]
        private static void ScheduleRepair()
        {
            EditorApplication.delayCall += RepairActiveLevel1IfNeeded;
        }

        [MenuItem("NHN Hackathon/Setup/Repair Level1 Ending Credits References")]
        public static void RepairLevel1()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (!TryFindReferences(
                    scene,
                    out GameSuccessController successController,
                    out EndingCreditsController endingController,
                    out GameObject endingRoot,
                    out RectTransform viewport,
                    out RectTransform credits,
                    out Image fade))
            {
                throw new InvalidOperationException(
                    "Level1 requires GameSuccessSystem, EndingCreditsSystem and EndingCreditsUI prefab instances.");
            }

            SerializedObject endingValues = new(endingController);
            endingValues.FindProperty("endingRoot").objectReferenceValue = endingRoot;
            endingValues.FindProperty("creditsViewport").objectReferenceValue = viewport;
            endingValues.FindProperty("creditsContent").objectReferenceValue = credits;
            endingValues.FindProperty("fadeImage").objectReferenceValue = fade;
            endingValues.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject successValues = new(successController);
            successValues.FindProperty("endingCreditsController").objectReferenceValue = endingController;
            successValues.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.RecordPrefabInstancePropertyModifications(endingController);
            PrefabUtility.RecordPrefabInstancePropertyModifications(successController);
            EditorUtility.SetDirty(endingController);
            EditorUtility.SetDirty(successController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("LEVEL1_ENDING_REFERENCES_REPAIRED: Ending system, UI and success controller are connected.");
        }

        private static void RepairActiveLevel1IfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath
                || !TryFindReferences(
                    scene,
                    out GameSuccessController successController,
                    out EndingCreditsController endingController,
                    out GameObject endingRoot,
                    out RectTransform viewport,
                    out RectTransform credits,
                    out Image fade))
            {
                return;
            }

            SerializedObject endingValues = new(endingController);
            SerializedObject successValues = new(successController);
            bool needsRepair = endingValues.FindProperty("endingRoot").objectReferenceValue != endingRoot
                || endingValues.FindProperty("creditsViewport").objectReferenceValue != viewport
                || endingValues.FindProperty("creditsContent").objectReferenceValue != credits
                || endingValues.FindProperty("fadeImage").objectReferenceValue != fade
                || successValues.FindProperty("endingCreditsController").objectReferenceValue != endingController;

            if (needsRepair)
            {
                RepairLevel1();
            }
        }

        private static bool TryFindReferences(
            Scene scene,
            out GameSuccessController successController,
            out EndingCreditsController endingController,
            out GameObject endingRoot,
            out RectTransform viewport,
            out RectTransform credits,
            out Image fade)
        {
            successController = null;
            endingController = null;
            endingRoot = null;
            viewport = null;
            credits = null;
            fade = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                successController ??= root.GetComponentInChildren<GameSuccessController>(true);
                endingController ??= root.GetComponentInChildren<EndingCreditsController>(true);

                Transform endingUI = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => value.name == "EndingCreditsUI");
                if (endingUI == null)
                {
                    continue;
                }

                endingRoot = endingUI.gameObject;
                viewport = FindChild<RectTransform>(endingUI, "CreditsViewport");
                credits = FindChild<RectTransform>(endingUI, "CreditsText");
                fade = FindChild<Image>(endingUI, "FadeImage");
            }

            return successController != null
                && endingController != null
                && endingRoot != null
                && viewport != null
                && credits != null
                && fade != null;
        }

        private static T FindChild<T>(Transform root, string objectName) where T : Component
        {
            return root.GetComponentsInChildren<T>(true)
                .FirstOrDefault(value => value.name == objectName);
        }
    }
}
#endif
