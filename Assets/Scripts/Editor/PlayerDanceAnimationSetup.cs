#if UNITY_EDITOR
using NHNHackathon.Dance;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class PlayerDanceAnimationSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Characters/Player.prefab";
        private const string ControllerPath = "Assets/Art/Character/DancingAI.controller";
        private static readonly string[] ClipPaths =
        {
            "Assets/Art/Character/Dance1.anim",
            "Assets/Art/Character/Dance2.anim",
            "Assets/Art/Character/Dance3.anim",
            "Assets/Art/Character/Dance4.anim"
        };

        [MenuItem("Tools/NHN Hackathon/Characters/Apply Player Dance Animations")]
        public static void Build()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                throw new System.InvalidOperationException("DancingAI.controller was not found.");

            AnimationClip[] clips = new AnimationClip[ClipPaths.Length];
            for (int index = 0; index < ClipPaths.Length; index++)
            {
                clips[index] = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPaths[index]);
                if (clips[index] == null)
                    throw new System.InvalidOperationException($"Missing clip: {ClipPaths[index]}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                DanceColorVisualizer color = root.GetComponent<DanceColorVisualizer>();
                if (color != null) Object.DestroyImmediate(color);

                Transform model = root.transform.Find("CharacterModel");
                if (model == null)
                    throw new System.InvalidOperationException("Player/CharacterModel was not found.");
                Animator animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                PlayerDanceAnimationPresenter presenter =
                    root.GetComponent<PlayerDanceAnimationPresenter>();
                if (presenter == null) presenter = root.AddComponent<PlayerDanceAnimationPresenter>();
                SerializedObject values = new(presenter);
                values.FindProperty("animator").objectReferenceValue = animator;
                SerializedProperty mappings = values.FindProperty("danceAnimations");
                mappings.arraySize = clips.Length;
                for (int index = 0; index < clips.Length; index++)
                {
                    SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
                    mapping.FindPropertyRelative("danceId").intValue = index + 1;
                    mapping.FindPropertyRelative("animationClip").objectReferenceValue = clips[index];
                    mapping.FindPropertyRelative("playbackSpeed").floatValue = 1f;
                }
                values.FindProperty("transitionDuration").floatValue = 0.2f;
                values.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("PLAYER_DANCE_ANIMATION_COMPLETE: Dance1-Dance4 applied; color feedback removed.");
        }
    }
}
#endif
