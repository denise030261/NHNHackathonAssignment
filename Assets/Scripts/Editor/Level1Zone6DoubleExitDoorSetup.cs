#if UNITY_EDITOR
using System;
using System.Linq;
using NHNHackathon.ExitSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1Zone6DoubleExitDoorSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string LeftPivotName = "ExitDoor_LeftPivot";
        private const string RightPivotName = "ExitDoor_RightPivot";

        [MenuItem("NHN Hackathon/Setup/Level1 Zone6 Double Exit Door")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Transform zone6 = FindSceneTransform(scene, "Zone6");
            if (zone6 == null)
            {
                throw new InvalidOperationException("Zone6 was not found in Level1.");
            }

            ExitDoor door = FindTargetExitDoor(zone6);
            Transform cube001 = FindSceneTransform(scene, "Cube.001");
            Transform cube077 = FindSceneTransform(scene, "Cube.077");
            if (door == null || cube001 == null || cube077 == null)
            {
                throw new InvalidOperationException(
                    "Zone6 ExitDoor and Background/Cube.001, Cube.077 are required.");
            }

            Transform oldPanel = GetObjectReference<Transform>(door, "doorPanel");
            Transform firstPivot = GetOrCreateOuterHinge(cube001, cube077, LeftPivotName);
            Transform secondPivot = GetOrCreateOuterHinge(cube077, cube001, RightPivotName);

            SerializedObject doorValues = new(door);
            doorValues.FindProperty("doorPanel").objectReferenceValue = firstPivot;
            doorValues.FindProperty("secondaryDoorPanel").objectReferenceValue = secondPivot;
            doorValues.FindProperty("openAngle").floatValue = 90f;
            doorValues.FindProperty("secondaryOpenAngle").floatValue = -90f;
            doorValues.ApplyModifiedPropertiesWithoutUndo();

            DisablePrototypeVisual(oldPanel, firstPivot, secondPivot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            EditorUtility.SetDirty(door);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"LEVEL1_ZONE6_DOUBLE_EXIT_COMPLETE: {door.name} opens Cube.001 and Cube.077 in opposite directions.");
        }

        private static ExitDoor FindTargetExitDoor(Transform zone6)
        {
            ExitDoor[] doors = zone6.GetComponentsInChildren<ExitDoor>(true);
            return doors.FirstOrDefault(value => value.name == "ExitDoor (1)")
                ?? doors.FirstOrDefault(value => value.GetComponent<StagedExitUnlockController>() != null)
                ?? doors.FirstOrDefault();
        }

        private static Transform GetOrCreateOuterHinge(
            Transform panel,
            Transform otherPanel,
            string pivotName)
        {
            if (panel.parent != null && panel.parent.name == pivotName)
            {
                return panel.parent;
            }

            Renderer renderer = panel.GetComponent<Renderer>()
                ?? panel.GetComponentInChildren<Renderer>(true);
            Renderer otherRenderer = otherPanel.GetComponent<Renderer>()
                ?? otherPanel.GetComponentInChildren<Renderer>(true);
            if (renderer == null || otherRenderer == null)
            {
                throw new InvalidOperationException("Both exit door meshes require a Renderer.");
            }

            Vector3 outward = renderer.bounds.center - otherRenderer.bounds.center;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = panel.right;
                outward.y = 0f;
            }
            outward.Normalize();

            Bounds bounds = renderer.bounds;
            float extent = Mathf.Abs(outward.x) * bounds.extents.x
                + Mathf.Abs(outward.y) * bounds.extents.y
                + Mathf.Abs(outward.z) * bounds.extents.z;
            Vector3 hingePosition = bounds.center + outward * extent;

            Transform originalParent = panel.parent;
            GameObject pivotObject = new(pivotName);
            Transform pivot = pivotObject.transform;
            pivot.SetParent(originalParent, false);
            pivot.position = hingePosition;
            pivot.rotation = originalParent != null ? originalParent.rotation : Quaternion.identity;
            pivot.localScale = Vector3.one;
            panel.SetParent(pivot, true);
            return pivot;
        }

        private static void DisablePrototypeVisual(
            Transform oldPanel,
            Transform firstPivot,
            Transform secondPivot)
        {
            if (oldPanel == null || oldPanel == firstPivot || oldPanel == secondPivot)
            {
                return;
            }

            foreach (Renderer renderer in oldPanel.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static T GetObjectReference<T>(UnityEngine.Object target, string propertyName)
            where T : UnityEngine.Object
        {
            SerializedObject values = new(target);
            return values.FindProperty(propertyName).objectReferenceValue as T;
        }

        private static Transform FindSceneTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => value.name == objectName);
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }
    }
}
#endif
