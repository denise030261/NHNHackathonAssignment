#if UNITY_EDITOR
using NHNHackathon.Cinematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    [CustomEditor(typeof(ProgressionToppleSequence))]
    public sealed class ProgressionToppleSequenceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Each Fallen Pose marker stores the exact final position and rotation. " +
                "Move or rotate the marker in Scene View to edit the result.",
                MessageType.Info);

            if (GUILayout.Button("Create Missing Fallen Pose Markers"))
            {
                CreateMissingPoseMarkers((ProgressionToppleSequence)target);
            }
        }

        [MenuItem("Tools/NHN Hackathon/Level1/Create Toies Fallen Pose Markers")]
        private static void CreateLevel1Markers()
        {
            foreach (ProgressionToppleSequence sequence in
                     Object.FindObjectsByType<ProgressionToppleSequence>(
                         FindObjectsInactive.Include))
            {
                CreateMissingPoseMarkers(sequence);
            }
        }

        public static void CreateMissingPoseMarkers(
            ProgressionToppleSequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            SerializedObject values = new(sequence);
            SerializedProperty targets = values.FindProperty("targets");
            float floorClearance = values.FindProperty("floorClearance").floatValue;
            Transform container = FindOrCreateContainer(sequence.transform);

            for (int index = 0; index < targets.arraySize; index++)
            {
                SerializedProperty entry = targets.GetArrayElementAtIndex(index);
                SerializedProperty poseProperty = entry.FindPropertyRelative("fallenPose");
                if (poseProperty.objectReferenceValue != null)
                {
                    continue;
                }

                Transform toppleTarget =
                    entry.FindPropertyRelative("target").objectReferenceValue as Transform;
                if (toppleTarget == null
                    || !TryGetWorldBounds(toppleTarget, out Bounds bounds))
                {
                    continue;
                }

                Vector3 fallEuler =
                    entry.FindPropertyRelative("fallEulerAngles").vector3Value;
                CalculateFallenPose(
                    toppleTarget, bounds, fallEuler, floorClearance,
                    out Vector3 position, out Quaternion rotation);

                GameObject markerObject = new($"{toppleTarget.name}_FallenPose");
                Undo.RegisterCreatedObjectUndo(markerObject, "Create Fallen Pose Marker");
                Transform marker = markerObject.transform;
                marker.SetParent(container, true);
                marker.SetPositionAndRotation(position, rotation);
                poseProperty.objectReferenceValue = marker;
            }

            values.ApplyModifiedProperties();
            EditorUtility.SetDirty(sequence);
            if (sequence.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);
            }
        }

        private static Transform FindOrCreateContainer(Transform sequenceRoot)
        {
            Transform container = sequenceRoot.Find("TopplePoseMarkers");
            if (container != null)
            {
                return container;
            }

            GameObject containerObject = new("TopplePoseMarkers");
            Undo.RegisterCreatedObjectUndo(containerObject, "Create Pose Marker Container");
            container = containerObject.transform;
            container.SetParent(sequenceRoot, false);
            return container;
        }

        private static void CalculateFallenPose(
            Transform target, Bounds bounds, Vector3 fallEuler,
            float floorClearance, out Vector3 position, out Quaternion rotation)
        {
            Vector3 localAxis = new(fallEuler.x, 0f, fallEuler.z);
            if (localAxis.sqrMagnitude < 0.001f)
            {
                localAxis = Vector3.right;
            }

            Vector3 worldAxis = target.TransformDirection(localAxis.normalized);
            worldAxis.y = 0f;
            worldAxis = worldAxis.sqrMagnitude < 0.001f
                ? Vector3.right
                : worldAxis.normalized;
            Vector3 fallDirection = Vector3.Cross(worldAxis, Vector3.up).normalized;
            float horizontalRadius = Mathf.Abs(fallDirection.x) * bounds.extents.x
                + Mathf.Abs(fallDirection.z) * bounds.extents.z;
            Vector3 pivot = new(
                bounds.center.x + fallDirection.x * horizontalRadius,
                bounds.min.y + floorClearance,
                bounds.center.z + fallDirection.z * horizontalRadius);

            float fallAngle = Mathf.Clamp(
                new Vector2(fallEuler.x, fallEuler.z).magnitude, 0f, 90f);
            Quaternion deltaRotation = Quaternion.AngleAxis(fallAngle, worldAxis);
            position = pivot + deltaRotation * (target.position - pivot);
            rotation = deltaRotation * target.rotation;
        }

        private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return true;
        }
    }
}
#endif
