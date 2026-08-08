#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.AudioSystem;
using NHNHackathon.Cinematics;
using NHNHackathon.Game;
using NHNHackathon.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class GameSfxSetup
    {
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        private const string Level1Scene = "Assets/Scenes/Level1.unity";
        private const string DancingPrefab = "Assets/Prefabs/Characters/DancingAI.prefab";
        private const string PlayerPrefab = "Assets/Prefabs/Characters/Player.prefab";
        private const string ScreamPath = "Assets/Audio/SFX/Screaming.wav";
        private const string TopplePath = "Assets/Audio/SFX/물체떨어지는소리.mp3";
        private const string HoverPath = "Assets/Audio/SFX/UI_Hovered.wav";
        private const string ClickPath = "Assets/Audio/SFX/UI_Click.wav";

        [MenuItem("NHN Hackathon/Setup/Game SFX")]
        public static void Build()
        {
            ConfigureImporter(ScreamPath, AudioClipLoadType.DecompressOnLoad);
            ConfigureImporter(TopplePath, AudioClipLoadType.CompressedInMemory);
            ConfigureImporter(HoverPath, AudioClipLoadType.DecompressOnLoad);
            ConfigureImporter(ClickPath, AudioClipLoadType.DecompressOnLoad);
            GameSfxLibrarySetup.Build();
            ConfigureDanceSfxReceiver(DancingPrefab, true);
            ConfigureDanceSfxReceiver(PlayerPrefab, false);
            ConfigureScene(MainMenuScene, false);
            ConfigureScene(Level1Scene, true);
            AssetDatabase.SaveAssets();
            Debug.Log("GAME_SFX_SETUP_COMPLETE Capture, Topple, Random Dance Animation, UI Hover/Click, World SFX Library");
        }

        private static void ConfigureDanceSfxReceiver(string prefabPath, bool requireSharedZone)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Animator animator = root.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    throw new System.InvalidOperationException($"Animator not found: {prefabPath}");
                RandomAnimationSfxEmitter receiver =
                    animator.GetComponent<RandomAnimationSfxEmitter>()
                    ?? animator.gameObject.AddComponent<RandomAnimationSfxEmitter>();
                SerializedObject values = new SerializedObject(receiver);
                values.FindProperty("requireSharedZone").boolValue = requireSharedZone;
                values.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureScene(string scenePath, bool gameplay)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AudioSource source = FindOrCreateSfxSource(scene);
            UISfxPlayer uiPlayer = source.GetComponent<UISfxPlayer>();
            if (uiPlayer == null) uiPlayer = source.gameObject.AddComponent<UISfxPlayer>();
            if (source.GetComponent<SfxVolumeInitializer>() == null)
                source.gameObject.AddComponent<SfxVolumeInitializer>();
            SerializedObject uiValues = new SerializedObject(uiPlayer);
            uiValues.FindProperty("hoverClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(HoverPath);
            uiValues.FindProperty("clickClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
            uiValues.ApplyModifiedPropertiesWithoutUndo();

            foreach (Button button in Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UIButtonHoverSfx hover = button.GetComponent<UIButtonHoverSfx>();
                if (hover == null) hover = button.gameObject.AddComponent<UIButtonHoverSfx>();
                SerializedObject hoverValues = new SerializedObject(hover);
                hoverValues.FindProperty("selectable").objectReferenceValue = button;
                hoverValues.ApplyModifiedPropertiesWithoutUndo();
            }

            if (gameplay)
            {
                AudioClip scream = AssetDatabase.LoadAssetAtPath<AudioClip>(ScreamPath);
                foreach (EnemyCaptureDirector director in Object.FindObjectsByType<EnemyCaptureDirector>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    SerializedObject values = new SerializedObject(director);
                    values.FindProperty("sfxSource").objectReferenceValue = source;
                    values.FindProperty("screamingSfx").objectReferenceValue = scream;
                    values.ApplyModifiedPropertiesWithoutUndo();
                }

                AudioClip topple = AssetDatabase.LoadAssetAtPath<AudioClip>(TopplePath);
                foreach (ProgressionToppleSequence sequence in Object.FindObjectsByType<ProgressionToppleSequence>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    SerializedObject values = new SerializedObject(sequence);
                    values.FindProperty("sfxSource").objectReferenceValue = source;
                    values.FindProperty("toppleSfx").objectReferenceValue = topple;
                    values.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            AudioSource[] sfxSources = Object.FindObjectsByType<AudioSource>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(value => value.GetComponent<NHNHackathon.AudioSystem.SceneBgmPlayer>() == null)
                .ToArray();
            foreach (AudioSettingsController settings in Object.FindObjectsByType<AudioSettingsController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject values = new SerializedObject(settings);
                SerializedProperty list = values.FindProperty("sfxSources");
                list.arraySize = sfxSources.Length;
                for (int index = 0; index < sfxSources.Length; index++)
                    list.GetArrayElementAtIndex(index).objectReferenceValue = sfxSources[index];
                values.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static AudioSource FindOrCreateSfxSource(Scene scene)
        {
            AudioSource source = Object.FindObjectsByType<AudioSource>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(value => value.name is "SFX AudioSource" or "Menu SFX AudioSource");
            if (source == null)
            {
                GameObject audioRoot = scene.GetRootGameObjects().FirstOrDefault(value => value.name == "Audio")
                    ?? new GameObject("Audio");
                source = new GameObject("SFX AudioSource").AddComponent<AudioSource>();
                source.transform.SetParent(audioRoot.transform, false);
            }
            ConfigureSource(source, false);
            return source;
        }

        private static void ConfigureSource(AudioSource source, bool spatial)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatial ? 1f : 0f;
            source.dopplerLevel = 0f;
        }

        private static void ConfigureImporter(string path, AudioClipLoadType loadType)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new System.InvalidOperationException($"Missing SFX: {path}");
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            settings.compressionFormat = loadType == AudioClipLoadType.DecompressOnLoad
                ? AudioCompressionFormat.PCM
                : AudioCompressionFormat.Vorbis;
            settings.quality = 0.75f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }
    }
}
#endif
