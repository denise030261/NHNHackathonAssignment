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
    public static class Level1Zone2BigDoorHingeSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string MotionRootName = "BigDoorMotionRoot";
        private const string LeftPivotName = "BigDoor_LeftPivot";
        private const string RightPivotName = "BigDoor_RightPivot";

        [MenuItem("NHN Hackathon/Setup/Level1 Zone2 Big Door Hinges")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Transform zone2 = FindSceneTransform(scene, "Zone2");
            ExitDoor door = FindBigDoorController(zone2);
            if (zone2 == null || door == null)
            {
                throw new InvalidOperationException("Zone2/ExitDoor containing BigDoor (1) was not found.");
            }

            SerializedObject values = new(door);
            Transform firstPanel = values.FindProperty("doorPanel").objectReferenceValue as Transform;
            Transform secondPanel = values.FindProperty("secondaryDoorPanel").objectReferenceValue as Transform;
            if (firstPanel == null || secondPanel == null || firstPanel == secondPanel)
            {
                throw new InvalidOperationException("BigDoor requires two different panel references.");
            }

            if (firstPanel.parent != null && firstPanel.parent.name == LeftPivotName
                && secondPanel.parent != null && secondPanel.parent.name == RightPivotName)
            {
                Debug.Log("LEVEL1_ZONE2_BIG_DOOR_COMPLETE: BigDoor hinges are already configured.");
                return;
            }

            Transform modelRoot = FindAncestor(firstPanel, "BigDoor (1)");
            if (modelRoot != null && PrefabUtility.IsPartOfPrefabInstance(modelRoot.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(
                    modelRoot.gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);

                values.UpdateIfRequiredOrScript();
                firstPanel = values.FindProperty("doorPanel").objectReferenceValue as Transform;
                secondPanel = values.FindProperty("secondaryDoorPanel").objectReferenceValue as Transform;
            }

            if (firstPanel == null || secondPanel == null)
            {
                throw new InvalidOperationException("BigDoor panel references were lost while unpacking.");
            }

            Transform motionRoot = zone2.Find(MotionRootName);
            if (motionRoot == null)
            {
                motionRoot = new GameObject(MotionRootName).transform;
                motionRoot.SetParent(zone2, false);
            }
            motionRoot.localPosition = Vector3.zero;
            motionRoot.localRotation = Quaternion.identity;
            motionRoot.localScale = Vector3.one;

            Transform firstPivot = CreateOuterHinge(
                firstPanel, secondPanel, motionRoot, LeftPivotName);
            Transform secondPivot = CreateOuterHinge(
                secondPanel, firstPanel, motionRoot, RightPivotName);

            values.UpdateIfRequiredOrScript();
            values.FindProperty("doorPanel").objectReferenceValue = firstPivot;
            values.FindProperty("secondaryDoorPanel").objectReferenceValue = secondPivot;
            values.FindProperty("openAngle").floatValue = 90f;
            values.FindProperty("secondaryOpenAngle").floatValue = -90f;
            values.ApplyModifiedPropertiesWithoutUndo();

            DisablePrototypeVisual(door.transform, firstPivot, secondPivot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            EditorUtility.SetDirty(door);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "LEVEL1_ZONE2_BIG_DOOR_COMPLETE: BigDoor panels now rotate from outer hinges under a uniform-scale motion root.");
        }

        private static ExitDoor FindBigDoorController(Transform zone2)
        {
            if (zone2 == null)
            {
                return null;
            }

            return zone2.GetComponentsInChildren<ExitDoor>(true)
                .FirstOrDefault(value => value.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.name == "BigDoor (1)"));
        }

        private static Transform CreateOuterHinge(
            Transform panel,
            Transform otherPanel,
            Transform motionRoot,
            string pivotName)
        {
            Renderer renderer = panel.GetComponent<Renderer>()
                ?? panel.GetComponentInChildren<Renderer>(true);
            Renderer otherRenderer = otherPanel.GetComponent<Renderer>()
                ?? otherPanel.GetComponentInChildren<Renderer>(true);
            if (renderer == null || otherRenderer == null)
            {
                throw new InvalidOperationException("Both BigDoor panels require a Renderer.");
            }

            Vector3 outward = renderer.bounds.center - otherRenderer.bounds.center;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
            {
                throw new InvalidOperationException("BigDoor panel centers overlap; hinge direction is undefined.");
            }
            outward.Normalize();

            Bounds bounds = renderer.bounds;
            float extent = Mathf.Abs(outward.x) * bounds.extents.x
                + Mathf.Abs(outward.z) * bounds.extents.z;
            Vector3 hingePosition = bounds.center + outward * extent;

            Transform pivot = new GameObject(pivotName).transform;
            pivot.SetParent(motionRoot, false);
            pivot.position = hingePosition;
            pivot.rotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            panel.SetParent(pivot, true);
            return pivot;
        }

        private static void DisablePrototypeVisual(
            Transform doorRoot,
            Transform firstPivot,
            Transform secondPivot)
        {
            Transform prototype = doorRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "DoorPanel"
                    && value != firstPivot && value != secondPivot);
            if (prototype == null)
            {
                return;
            }

            foreach (Renderer renderer in prototype.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static Transform FindAncestor(Transform value, string objectName)
        {
            while (value != null)
            {
                if (value.name == objectName)
                {
                    return value;
                }
                value = value.parent;
            }
            return null;
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
