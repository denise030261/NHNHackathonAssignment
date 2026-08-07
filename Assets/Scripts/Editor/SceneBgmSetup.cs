#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.AudioSystem;
using NHNHackathon.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class SceneBgmSetup
    {
        private readonly struct SceneBgmEntry
        {
            public SceneBgmEntry(string scenePath, string clipPath)
            {
                ScenePath = scenePath;
                ClipPath = clipPath;
            }

            public string ScenePath { get; }
            public string ClipPath { get; }
        }

        private static readonly SceneBgmEntry[] Entries =
        {
            new("Assets/Scenes/MainMenu.unity", "Assets/Audio/BGM/MainMenu.mp3"),
            new("Assets/Scenes/Level1.unity", "Assets/Audio/BGM/Level1.mp3")
        };

        [MenuItem("NHN Hackathon/Setup/Scene BGM")]
        public static void Build()
        {
            foreach (SceneBgmEntry entry in Entries)
            {
                ConfigureImporter(entry.ClipPath);
                ConfigureScene(entry);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("SCENE_BGM_SETUP_COMPLETE MainMenu=MainMenu.mp3, Level1=Level1.mp3");
        }

        private static void ConfigureScene(SceneBgmEntry entry)
        {
            Scene scene = EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.ClipPath)
                ?? throw new System.InvalidOperationException($"BGM clip was not found: {entry.ClipPath}");

            AudioSource source = Object.FindObjectsByType<AudioSource>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(value => value.name == "BGM AudioSource");
            if (source == null)
            {
                GameObject audioRoot = scene.GetRootGameObjects()
                    .FirstOrDefault(value => value.name == "Audio");
                if (audioRoot == null) audioRoot = new GameObject("Audio");
                GameObject sourceObject = new GameObject("BGM AudioSource");
                sourceObject.transform.SetParent(audioRoot.transform, false);
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            SceneBgmPlayer player = source.GetComponent<SceneBgmPlayer>()
                ?? source.gameObject.AddComponent<SceneBgmPlayer>();
            SerializedObject playerValues = new SerializedObject(player);
            playerValues.FindProperty("clip").objectReferenceValue = clip;
            playerValues.FindProperty("playOnStart").boolValue = true;
            playerValues.FindProperty("loop").boolValue = true;
            playerValues.FindProperty("fadeInDuration").floatValue = 0.75f;
            playerValues.ApplyModifiedPropertiesWithoutUndo();

            foreach (AudioSettingsController settings in Object.FindObjectsByType<AudioSettingsController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject settingsValues = new SerializedObject(settings);
                settingsValues.FindProperty("bgmSource").objectReferenceValue = source;
                settingsValues.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureImporter(string clipPath)
        {
            AudioImporter importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
            if (importer == null) return;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }
    }
}
#endif
