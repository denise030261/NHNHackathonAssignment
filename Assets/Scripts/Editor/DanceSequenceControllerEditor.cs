#if UNITY_EDITOR
using NHNHackathon.AI;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    [CustomEditor(typeof(DanceSequenceController))]
    public sealed class DanceSequenceControllerEditor : Editor
    {
        private SerializedProperty beatInterval;
        private SerializedProperty danceCatalog;
        private ReorderableList danceSequence;

        private void OnEnable()
        {
            beatInterval = serializedObject.FindProperty("beatInterval");
            danceCatalog = serializedObject.FindProperty("danceCatalog");
            SerializedProperty sequenceProperty = serializedObject.FindProperty("danceSequence");
            danceSequence = new ReorderableList(serializedObject, sequenceProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Dance Sequence (drag to reorder)"),
                elementHeight = EditorGUIUtility.singleLineHeight + 6f,
                drawElementCallback = DrawDanceStep
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(danceCatalog);
            EditorGUILayout.PropertyField(beatInterval);
            EditorGUILayout.Space();
            danceSequence.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDanceStep(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty step = danceSequence.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                step, new GUIContent($"Step {index + 1} Dance ID"));
        }
    }
}
#endif
