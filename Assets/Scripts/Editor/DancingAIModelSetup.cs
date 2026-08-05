#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NHNHackathon.AI;
using NHNHackathon.Dance;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class DancingAIModelSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Characters/DancingAI.prefab";
        private const string ModelPath = "Assets/Art/Character/Manny2_Anim.fbx";
        private const string ControllerPath = "Assets/Art/Character/DancingAI.controller";
        private static readonly string[] AnimationPaths =
        {
            "Assets/Art/Character/Dance1.anim",
            "Assets/Art/Character/Dance2.anim",
            "Assets/Art/Character/Dance3.anim",
            "Assets/Art/Character/Dance4.anim"
        };

        [MenuItem("Tools/NHN Hackathon/Characters/Apply Dancing AI Model")]
        public static void Build()
        {
            AnimationClip[] clips = LoadAnimationClips();
            if (clips.Length == 0)
            {
                throw new System.InvalidOperationException("Manny2_Anim.fbx contains no animation clips.");
            }

            AnimatorController controller = BuildController(clips);
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform oldModel = root.transform.Find("CharacterModel");
                if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);
                Transform prototype = root.transform.Find("Visual");
                if (prototype != null) prototype.gameObject.SetActive(false);

                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                model.name = "CharacterModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                NormalizeModel(model, 2f);

                Animator animator = model.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                    .OfType<Avatar>().FirstOrDefault();
                animator.applyRootMotion = false;

                SkinnedMeshRenderer renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
                DanceColorVisualizer color = root.GetComponent<DanceColorVisualizer>();
                AIDanceColorPresenter colorPresenter = root.GetComponent<AIDanceColorPresenter>();
                if (colorPresenter != null) Object.DestroyImmediate(colorPresenter);
                if (color != null) Object.DestroyImmediate(color);

                AIDanceAnimationPresenter presenter = root.GetComponent<AIDanceAnimationPresenter>();
                if (presenter == null) presenter = root.AddComponent<AIDanceAnimationPresenter>();
                ConfigurePresenter(presenter, animator, clips);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"DANCING_AI_MODEL_COMPLETE: {clips.Length} clip(s) applied: "
                + string.Join(", ", clips.Select(clip => clip.name)));
        }

        private static AnimationClip[] LoadAnimationClips() => AnimationPaths
            .Select(AssetDatabase.LoadAssetAtPath<AnimationClip>)
            .Where(clip => clip != null)
            .ToArray();

        private static AnimatorController BuildController(AnimationClip[] clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states)
            {
                machine.RemoveState(child.state);
            }
            foreach (AnimationClip clip in clips)
            {
                AnimatorState state = machine.AddState(clip.name);
                state.motion = clip;
                if (machine.defaultState == null) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigurePresenter(
            AIDanceAnimationPresenter presenter, Animator animator, AnimationClip[] clips)
        {
            SerializedObject values = new(presenter);
            values.FindProperty("animator").objectReferenceValue = animator;
            SerializedProperty mappings = values.FindProperty("danceAnimations");
            mappings.arraySize = 4;
            for (int index = 0; index < mappings.arraySize; index++)
            {
                SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
                mapping.FindPropertyRelative("danceId").intValue = index + 1;
                mapping.FindPropertyRelative("animationClip").objectReferenceValue = clips[index];
                mapping.FindPropertyRelative("playbackSpeed").floatValue = 1f;
            }
            values.FindProperty("transitionDuration").floatValue = 0.2f;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeModel(GameObject model, float targetHeight)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float scale = bounds.size.y > 0.001f ? targetHeight / bounds.size.y : 1f;
            model.transform.localScale = Vector3.one * scale;
            Bounds scaledBounds = model.GetComponentsInChildren<Renderer>(true)[0].bounds;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true).Skip(1))
                scaledBounds.Encapsulate(renderer.bounds);
            model.transform.position += Vector3.up * (model.transform.position.y - scaledBounds.min.y);
        }

    }
}
#endif
