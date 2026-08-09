#if UNITY_EDITOR
using System;
using System.Linq;
using NHNHackathon.AI;
using NHNHackathon.AudioSystem;
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class PlayerCharacterAnimationSetup
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player.prefab";
        private const string DancingAIPrefabPath = "Assets/Prefabs/Characters/DancingAI.prefab";
        private const string BaseModelPath = "Assets/Art/Animations/Manny2_Idle.fbx";
        private const string FlashlightModelPath =
            "Assets/Art/Items/flashlight/Flashlight.fbx";
        private const string PlayerControllerPath = "Assets/Art/Animations/Player.controller";
        private const string DancingAIControllerPath = "Assets/Art/Character/DancingAI.controller";
        private const string GeneratedClipFolder = "Assets/Art/Animations/Generated";

        private static readonly string[] LocomotionPaths =
        {
            "Assets/Art/Animations/Manny2_Idle.fbx",
            "Assets/Art/Animations/Manny2_Walk.fbx",
            "Assets/Art/Animations/Manny2_FlashIdle.fbx",
            "Assets/Art/Animations/Manny2_FlashWalk.fbx"
        };

        private static readonly string[] DancePaths =
        {
            "Assets/Art/Animations/Dance1.anim",
            "Assets/Art/Animations/Dance2.anim",
            "Assets/Art/Animations/Dance3.anim",
            "Assets/Art/Animations/Dance4.anim"
        };

        [MenuItem("Tools/NHN Hackathon/Characters/Apply New Player And DancingAI Model")]
        public static void Build()
        {
            ConfigureLoopingClips();
            AnimationClip[] locomotionClips = LocomotionPaths
                .Select(LoadFirstAnimationClip).ToArray();
            AnimationClip[] danceClips = DancePaths
                .Select(AssetDatabase.LoadAssetAtPath<AnimationClip>).ToArray();
            EnsureAllClips(locomotionClips, LocomotionPaths);
            EnsureAllClips(danceClips, DancePaths);

            string modelRootPath = GetAnimationRootPath(locomotionClips[0]);
            locomotionClips = CreateCompatibleClips(
                locomotionClips,
                new[] { "Idle", "Walk", "FlashIdle", "FlashWalk" },
                modelRootPath);
            danceClips = CreateCompatibleClips(
                danceClips,
                new[] { "Dance1", "Dance2", "Dance3", "Dance4" },
                modelRootPath);

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(BaseModelPath)
                .OfType<Avatar>().FirstOrDefault();
            AnimatorController controller = BuildPlayerController(
                locomotionClips, danceClips);
            AnimatorController dancingAIController = BuildDancingAIController(danceClips);
            ApplyPlayerModel(controller, avatar, danceClips);
            ApplyDancingAIModelAndAnimations(
                avatar, dancingAIController, danceClips);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NEW_CHARACTER_ANIMATION_SETUP_COMPLETE: Player model/locomotion/flash/dances and DancingAI model applied.");
        }

        [MenuItem("Tools/NHN Hackathon/Characters/Sync Dance Animation Events")]
        public static void SyncDanceAnimationEvents()
        {
            EnsureGeneratedFolder();
            for (int index = 0; index < DancePaths.Length; index++)
            {
                AnimationClip source =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(DancePaths[index]);
                AnimationClip compatible = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    $"{GeneratedClipFolder}/Player_Dance{index + 1}.anim");
                if (source == null || compatible == null)
                {
                    continue;
                }

                AnimationUtility.SetAnimationEvents(
                    compatible, AnimationUtility.GetAnimationEvents(source));
                EditorUtility.SetDirty(compatible);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("DANCE_ANIMATION_EVENTS_SYNCED: Source Dance1-4 -> compatible Player/DancingAI clips.");
        }

        public static void ApplyDancingAIModelOnly()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(BaseModelPath)
                .OfType<Avatar>().FirstOrDefault();
            ApplyDancingAIModelOnly(avatar);
            AssetDatabase.SaveAssets();
            Debug.Log("NEW_DANCING_AI_MODEL_ONLY_COMPLETE: Existing dance mappings were preserved.");
        }

        private static void ConfigureLoopingClips()
        {
            foreach (string path in LocomotionPaths)
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }

                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip LoadFirstAnimationClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));

        private static void EnsureAllClips(AnimationClip[] clips, string[] paths)
        {
            for (int index = 0; index < clips.Length; index++)
            {
                if (clips[index] == null)
                {
                    throw new InvalidOperationException($"Animation clip was not found: {paths[index]}");
                }
            }
        }

        private static AnimatorController BuildPlayerController(
            AnimationClip[] locomotion, AnimationClip[] dances)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    PlayerControllerPath);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("FlashlightOn", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
            {
                machine.RemoveAnyStateTransition(transition);
            }
            foreach (ChildAnimatorState child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            foreach (BlendTree oldTree in AssetDatabase
                         .LoadAllAssetsAtPath(PlayerControllerPath)
                         .OfType<BlendTree>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(oldTree, true);
            }

            BlendTree locomotionTree = CreateLocomotionBlendTree(
                controller, "LocomotionBlend", locomotion[0], locomotion[1]);
            BlendTree flashlightTree = CreateLocomotionBlendTree(
                controller, "FlashlightLocomotionBlend", locomotion[2], locomotion[3]);
            AnimatorState locomotionState = AddState(
                machine, "Locomotion", locomotionTree);
            AnimatorState flashlightState = AddState(
                machine, "FlashlightLocomotion", flashlightTree);
            machine.defaultState = locomotionState;

            AddImmediateTransition(locomotionState, flashlightState,
                Condition(AnimatorConditionMode.If, 0f, "FlashlightOn"));
            AddImmediateTransition(flashlightState, locomotionState,
                Condition(AnimatorConditionMode.IfNot, 0f, "FlashlightOn"));

            for (int index = 0; index < dances.Length; index++)
            {
                AnimatorState danceState = AddState(
                    machine, $"Dance{index + 1}", dances[index]);
                AddDanceMovementExit(danceState, locomotionState, false);
                AddDanceMovementExit(danceState, flashlightState, true);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BlendTree CreateLocomotionBlendTree(
            AnimatorController controller, string name,
            AnimationClip idle, AnimationClip walk)
        {
            BlendTree tree = new()
            {
                name = name,
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 2f);
            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        private static AnimatorController BuildDancingAIController(
            AnimationClip[] dances)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(DancingAIControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    DancingAIControllerPath);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (AnimatorStateTransition transition in
                     machine.anyStateTransitions.ToArray())
            {
                machine.RemoveAnyStateTransition(transition);
            }
            foreach (ChildAnimatorState child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            for (int index = 0; index < dances.Length; index++)
            {
                AnimatorState state = AddState(
                    machine, $"Dance{index + 1}", dances[index]);
                if (index == 0)
                {
                    machine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip[] CreateCompatibleClips(
            AnimationClip[] sources, string[] names, string targetRootPath)
        {
            EnsureGeneratedFolder();
            AnimationClip[] results = new AnimationClip[sources.Length];
            for (int index = 0; index < sources.Length; index++)
            {
                string path = $"{GeneratedClipFolder}/Player_{names[index]}.anim";
                AnimationClip target = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (target == null)
                {
                    target = new AnimationClip();
                    AssetDatabase.CreateAsset(target, path);
                }

                CopyClipWithRebasedRoot(sources[index], target, names[index], targetRootPath);
                results[index] = target;
            }
            AssetDatabase.SaveAssets();
            return results;
        }

        private static void CopyClipWithRebasedRoot(
            AnimationClip source, AnimationClip target, string clipName,
            string targetRootPath)
        {
            EditorUtility.CopySerialized(source, target);
            target.name = clipName;

            EditorCurveBinding[] copiedCurves = AnimationUtility.GetCurveBindings(target);
            foreach (EditorCurveBinding binding in copiedCurves)
            {
                AnimationUtility.SetEditorCurve(target, binding, null);
            }
            foreach (EditorCurveBinding sourceBinding in AnimationUtility.GetCurveBindings(source))
            {
                EditorCurveBinding targetBinding = RebaseBinding(sourceBinding, targetRootPath);
                AnimationUtility.SetEditorCurve(
                    target, targetBinding, AnimationUtility.GetEditorCurve(source, sourceBinding));
            }

            EditorCurveBinding[] copiedObjectCurves =
                AnimationUtility.GetObjectReferenceCurveBindings(target);
            foreach (EditorCurveBinding binding in copiedObjectCurves)
            {
                AnimationUtility.SetObjectReferenceCurve(target, binding, null);
            }
            foreach (EditorCurveBinding sourceBinding in
                     AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                EditorCurveBinding targetBinding = RebaseBinding(sourceBinding, targetRootPath);
                AnimationUtility.SetObjectReferenceCurve(
                    target, targetBinding,
                    AnimationUtility.GetObjectReferenceCurve(source, sourceBinding));
            }

            AnimationUtility.SetAnimationEvents(
                target, AnimationUtility.GetAnimationEvents(source));
            EditorUtility.SetDirty(target);
        }

        private static EditorCurveBinding RebaseBinding(
            EditorCurveBinding binding, string targetRootPath)
        {
            if (string.IsNullOrEmpty(binding.path))
            {
                return binding;
            }

            int separator = binding.path.IndexOf('/');
            binding.path = separator >= 0
                ? targetRootPath + binding.path.Substring(separator)
                : targetRootPath;
            return binding;
        }

        private static string GetAnimationRootPath(AnimationClip modelClip)
        {
            string firstPath = AnimationUtility.GetCurveBindings(modelClip)
                .Select(binding => binding.path)
                .FirstOrDefault(path => !string.IsNullOrEmpty(path));
            if (string.IsNullOrEmpty(firstPath))
            {
                throw new InvalidOperationException(
                    $"No transform animation path was found in '{modelClip.name}'.");
            }

            int separator = firstPath.IndexOf('/');
            return separator >= 0 ? firstPath.Substring(0, separator) : firstPath;
        }

        private static void EnsureGeneratedFolder()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedClipFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art/Animations", "Generated");
            }
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine, string name, Motion motion)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimatorCondition Condition(
            AnimatorConditionMode mode, float threshold, string parameter) =>
            new() { mode = mode, threshold = threshold, parameter = parameter };

        private static void AddImmediateTransition(
            AnimatorState source, AnimatorState destination,
            params AnimatorCondition[] conditions)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.28f;
            foreach (AnimatorCondition condition in conditions)
            {
                transition.AddCondition(
                    condition.mode, condition.threshold, condition.parameter);
            }
        }

        private static void AddDanceMovementExit(
            AnimatorState dance, AnimatorState destination,
            bool flashlightOn)
        {
            AnimatorStateTransition transition = dance.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.3f;
            transition.AddCondition(
                AnimatorConditionMode.Greater, 0.05f, "Speed");
            transition.AddCondition(
                flashlightOn ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f, "FlashlightOn");
        }

        private static void ApplyPlayerModel(
            AnimatorController controller, Avatar avatar, AnimationClip[] danceClips)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ReplaceModel(root, controller, avatar, false, out GameObject model,
                    out Animator animator);

                Transform rightHand = FindTransform(model.transform,
                    "hand.R", "hand_r", "RightHand");
                Transform socket = new GameObject("ThirdPersonFlashlightSocket").transform;
                socket.SetParent(rightHand != null ? rightHand : model.transform, false);

                GameObject flashlightModel =
                    AssetDatabase.LoadAssetAtPath<GameObject>(FlashlightModelPath)
                    ?? throw new InvalidOperationException(
                        $"Model was not found: {FlashlightModelPath}");
                GameObject heldMesh = (GameObject)PrefabUtility.InstantiatePrefab(
                    flashlightModel, socket);
                heldMesh.name = "HeldFlashlightMesh";
                heldMesh.transform.SetLocalPositionAndRotation(
                    Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
                heldMesh.transform.localScale = Vector3.one * 1.25f;

                PlayerFlashlightController flashlight =
                    root.GetComponentInChildren<PlayerFlashlightController>(true);
                if (flashlight != null)
                {
                    SerializedObject flashlightValues = new(flashlight);
                    flashlightValues.FindProperty("thirdPersonParent").objectReferenceValue =
                        root.transform;
                    flashlightValues.FindProperty("thirdPersonLocalPosition").vector3Value =
                        new Vector3(0f, 1.35f, 0.4f);
                    flashlightValues.FindProperty("thirdPersonLocalEulerAngles").vector3Value = Vector3.zero;
                    flashlightValues.FindProperty("heldFlashlightMesh").objectReferenceValue =
                        heldMesh;
                    flashlightValues.FindProperty("heldFlashlightMeshScale").floatValue = 1.25f;
                    flashlightValues.ApplyModifiedPropertiesWithoutUndo();
                }

                PlayerLocomotionAnimationPresenter locomotion =
                    root.GetComponent<PlayerLocomotionAnimationPresenter>()
                    ?? root.AddComponent<PlayerLocomotionAnimationPresenter>();
                SerializedObject locomotionValues = new(locomotion);
                locomotionValues.FindProperty("animator").objectReferenceValue = animator;
                locomotionValues.FindProperty("characterController").objectReferenceValue =
                    root.GetComponent<CharacterController>();
                locomotionValues.FindProperty("flashlightController").objectReferenceValue = flashlight;
                locomotionValues.FindProperty("speedDampTime").floatValue = 0.18f;
                locomotionValues.FindProperty("locomotionStateName").stringValue =
                    "Locomotion";
                locomotionValues.FindProperty("flashlightLocomotionStateName").stringValue =
                    "FlashlightLocomotion";
                locomotionValues.FindProperty("flashlightTransitionDuration").floatValue = 0.2f;
                locomotionValues.ApplyModifiedPropertiesWithoutUndo();

                PlayerDanceAnimationPresenter dancePresenter =
                    root.GetComponent<PlayerDanceAnimationPresenter>()
                    ?? root.AddComponent<PlayerDanceAnimationPresenter>();
                ConfigurePlayerDancePresenter(dancePresenter, animator, danceClips);

                PlayerPerspectiveVisualController perspectiveVisual =
                    root.GetComponent<PlayerPerspectiveVisualController>()
                    ?? root.AddComponent<PlayerPerspectiveVisualController>();
                SerializedObject perspectiveVisualValues = new(perspectiveVisual);
                perspectiveVisualValues.FindProperty("cameraController").objectReferenceValue =
                    root.GetComponent<PlayerCameraController>();
                perspectiveVisualValues.FindProperty("characterModelRoot").objectReferenceValue =
                    model.transform;
                perspectiveVisualValues.FindProperty("keepShadowsInFirstPerson").boolValue = true;
                perspectiveVisualValues.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyDancingAIModelOnly(Avatar avatar)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(DancingAIPrefabPath);
            try
            {
                AIDanceAnimationPresenter presenter =
                    root.GetComponent<AIDanceAnimationPresenter>();
                Animator oldAnimator = root.GetComponentInChildren<Animator>(true);
                RuntimeAnimatorController existingController =
                    oldAnimator != null ? oldAnimator.runtimeAnimatorController : null;

                ReplaceModel(root, existingController, avatar, true,
                    out _, out Animator newAnimator);
                if (presenter != null)
                {
                    SerializedObject values = new(presenter);
                    values.FindProperty("animator").objectReferenceValue = newAnimator;
                    values.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, DancingAIPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyDancingAIModelAndAnimations(
            Avatar avatar, AnimatorController controller, AnimationClip[] danceClips)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(DancingAIPrefabPath);
            try
            {
                AIDanceAnimationPresenter presenter =
                    root.GetComponent<AIDanceAnimationPresenter>();
                ReplaceModel(root, controller, avatar, true,
                    out _, out Animator animator);
                if (presenter != null)
                {
                    SerializedObject values = new(presenter);
                    values.FindProperty("animator").objectReferenceValue = animator;
                    SerializedProperty mappings = values.FindProperty("danceAnimations");
                    mappings.arraySize = danceClips.Length;
                    for (int index = 0; index < danceClips.Length; index++)
                    {
                        SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
                        mapping.FindPropertyRelative("danceId").intValue = index + 1;
                        mapping.FindPropertyRelative("animationClip").objectReferenceValue =
                            danceClips[index];
                        mapping.FindPropertyRelative("playbackSpeed").floatValue = 1f;
                    }
                    values.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, DancingAIPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ReplaceModel(
            GameObject root, RuntimeAnimatorController controller, Avatar avatar,
            bool requireSharedZone, out GameObject model, out Animator animator)
        {
            Transform oldModel = root.transform.Find("CharacterModel");
            if (oldModel != null)
            {
                UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            }

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath)
                ?? throw new InvalidOperationException($"Model was not found: {BaseModelPath}");
            model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
            model.name = "CharacterModel";
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            NormalizeModel(model, 2f);

            animator = model.GetComponentInChildren<Animator>(true)
                ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;

            RandomAnimationSfxEmitter sfx =
                animator.GetComponent<RandomAnimationSfxEmitter>()
                ?? animator.gameObject.AddComponent<RandomAnimationSfxEmitter>();
            SerializedObject sfxValues = new(sfx);
            sfxValues.FindProperty("requireSharedZone").boolValue = requireSharedZone;
            sfxValues.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePlayerDancePresenter(
            PlayerDanceAnimationPresenter presenter, Animator animator,
            AnimationClip[] clips)
        {
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
            values.FindProperty("transitionDuration").floatValue = 0.28f;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindTransform(Transform root, params string[] names) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(
                value => names.Any(name =>
                    string.Equals(value.name, name, StringComparison.OrdinalIgnoreCase)));

        private static void NormalizeModel(GameObject model, float targetHeight)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            float scale = bounds.size.y > 0.001f ? targetHeight / bounds.size.y : 1f;
            model.transform.localScale = Vector3.one * scale;

            renderers = model.GetComponentsInChildren<Renderer>(true);
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            model.transform.position += Vector3.up *
                (model.transform.parent.position.y - bounds.min.y);
        }
    }
}
#endif
