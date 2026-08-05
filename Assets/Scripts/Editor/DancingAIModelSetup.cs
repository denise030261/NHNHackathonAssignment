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
        private const string ControllerFolder = "Assets/Animations/DancingAI";
        private const string ControllerPath = ControllerFolder + "/DancingAI.controller";

        [MenuItem("Tools/NHN Hackathon/Characters/Apply Dancing AI Model")]
        public static void Build()
        {
            ConfigureAnimationLoops();
            AnimationClip[] clips = LoadAnimationClips();
            if (clips.Length == 0)
            {
                throw new System.InvalidOperationException("Manny2_Anim.fbx contains no animation clips.");
            }

            EnsureFolder("Assets", "Animations");
            EnsureFolder("Assets/Animations", "DancingAI");
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
                SerializedObject colorValues = new(color);
                colorValues.FindProperty("targetRenderer").objectReferenceValue = renderer;
                colorValues.ApplyModifiedPropertiesWithoutUndo();

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

        private static void ConfigureAnimationLoops()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips) clip.loopTime = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip[] LoadAnimationClips() => AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToArray();

        private static AnimatorController BuildController(AnimationClip[] clips)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (AnimationClip clip in clips)
            {
                AnimatorState state = machine.AddState(clip.name);
                state.motion = clip;
                if (machine.defaultState == null) machine.defaultState = state;
            }
            return controller;
        }

        private static void ConfigurePresenter(
            AIDanceAnimationPresenter presenter, Animator animator, AnimationClip[] clips)
        {
            SerializedObject values = new(presenter);
            values.FindProperty("animator").objectReferenceValue = animator;
            SerializedProperty mappings = values.FindProperty("danceAnimations");
            mappings.arraySize = 6;
            for (int index = 0; index < mappings.arraySize; index++)
            {
                SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
                mapping.FindPropertyRelative("danceId").intValue = index + 1;
                mapping.FindPropertyRelative("animatorStateName").stringValue =
                    clips[index % clips.Length].name;
            }
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

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
