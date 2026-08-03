#if UNITY_EDITOR
using System;
using System.Linq;
using NHNHackathon.Enemy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.EditorTools
{
    public static class WatcherModelSetup
    {
        private const string ModelPath = "Assets/Art/Watcher/Watcher_Walk.fbx";
        private const string ControllerPath = "Assets/Art/Watcher/Watcher.controller";
        private const string PrefabPath = "Assets/Prefabs/Characters/Watcher.prefab";
        private const float TargetHeight = 2f;
        private const float FloorOffsetFromRoot = -1f;

        [MenuItem("Tools/NHN Hackathon/Characters/Apply Watcher Model")]
        public static void Build()
        {
            ConfigureModelImporter();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            AnimationClip walkClip = LoadWalkClip();
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            AnimatorController animatorController = CreateAnimatorController(walkClip);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ReplaceVisual(prefabRoot, model, avatar, animatorController);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Watcher model applied. Walk clip: {walkClip.name}");
        }

        private static void ConfigureModelImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Model importer not found: {ModelPath}");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importCameras = false;
            importer.importLights = false;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                throw new InvalidOperationException("Watcher FBX does not contain an animation clip.");
            }

            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.keepOriginalOrientation = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadWalkClip()
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(value => !value.name.StartsWith("__preview__", StringComparison.Ordinal))
                .Where(value => value.name.IndexOf("metarig", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(value => value.name.Length)
                .FirstOrDefault();
            return clip != null
                ? clip
                : throw new InvalidOperationException("No usable walk clip was found in Watcher_Walk.fbx.");
        }

        private static AnimatorController CreateAnimatorController(AnimationClip walkClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null || controller.layers.Length == 0)
            {
                if (controller != null)
                {
                    AssetDatabase.DeleteAsset(ControllerPath);
                }
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                stateMachine.RemoveState(child.state);
            }
            AnimatorState walkState = stateMachine.AddState("Walk");
            walkState.motion = walkClip;
            stateMachine.defaultState = walkState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ReplaceVisual(
            GameObject prefabRoot, GameObject model, Avatar avatar,
            RuntimeAnimatorController animatorController)
        {
            Transform oldVisual = prefabRoot.transform.Find("Visual");
            if (oldVisual != null)
            {
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Visual";
            visual.transform.SetParent(prefabRoot.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            SetLayerRecursively(visual, prefabRoot.layer);

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Watcher model has no Renderer.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            float scale = TargetHeight / Mathf.Max(bounds.size.y, 0.001f);
            visual.transform.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            float desiredFloor = prefabRoot.transform.position.y + FloorOffsetFromRoot;
            visual.transform.position += Vector3.up * (desiredFloor - bounds.min.y);

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = animatorController;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            NavMeshAgent agent = prefabRoot.GetComponent<NavMeshAgent>();
            CapsuleCollider collider = prefabRoot.GetComponent<CapsuleCollider>();
            if (collider != null)
            {
                collider.height = TargetHeight;
                collider.radius = 0.4f;
                collider.center = Vector3.zero;
            }
            WatcherAnimationController animation =
                prefabRoot.GetComponent<WatcherAnimationController>();
            if (animation == null)
            {
                animation = prefabRoot.AddComponent<WatcherAnimationController>();
            }
            SerializedObject animationSettings = new SerializedObject(animation);
            animationSettings.FindProperty("agent").objectReferenceValue = agent;
            animationSettings.FindProperty("animator").objectReferenceValue = animator;
            animationSettings.ApplyModifiedPropertiesWithoutUndo();

            EnemyController enemy = prefabRoot.GetComponent<EnemyController>();
            SerializedObject enemySettings = new SerializedObject(enemy);
            enemySettings.FindProperty("enemyRenderer").objectReferenceValue = renderers[0];
            enemySettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
#endif
