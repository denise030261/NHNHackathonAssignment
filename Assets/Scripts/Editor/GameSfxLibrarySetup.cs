#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NHNHackathon.AudioSystem;
using UnityEditor;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class GameSfxLibrarySetup
    {
        private const string ResourceFolder = "Assets/Resources";
        private const string LibraryPath = ResourceFolder + "/GameSfxLibrary.asset";

        [MenuItem("NHN Hackathon/Setup/Game SFX Library")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(ResourceFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            GameSfxLibrary library = AssetDatabase.LoadAssetAtPath<GameSfxLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<GameSfxLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            SerializedObject values = new(library);
            Assign(values, "keyPickup", "Assets/Audio/SFX/키줍는소리.mp3");
            Assign(values, "paperPickup", "Assets/Audio/SFX/종이줍는소리.mp3");
            Assign(values, "flashlightPickup", "Assets/Audio/SFX/손전등드는소리_[cut_0sec].mp3");
            Assign(values, "flashlightToggle", "Assets/Audio/SFX/손전등_[cut_0sec].mp3");
            Assign(values, "regularDoor", "Assets/Audio/SFX/문 여는 소리_[cut_2sec].mp3");
            Assign(values, "doorSlam", "Assets/Audio/SFX/문이 쾅 닫는 소리_[cut_0sec].mp3");
            Assign(values, "lightFlicker", "Assets/Audio/SFX/전등깜빡이는.mp3");
            Assign(values, "firstExitUnlock", "Assets/Audio/SFX/첫번째출구여는소리.mp3");
            AssignList(values, "dollMovementClips", new[]
            {
                "Assets/Audio/SFX/인형움직이는소리1.mp3",
                "Assets/Audio/SFX/인형움직이는소리2.mp3",
                "Assets/Audio/SFX/인형움직이는소리3.mp3",
                "Assets/Audio/SFX/인형움직이는소리4.mp3",
                "Assets/Audio/SFX/인형움직이는소리5.mp3",
                "Assets/Audio/SFX/인형움직이는소리6.mp3",
                "Assets/Audio/SFX/인형움직이는소리7.mp3",
                "Assets/Audio/SFX/인형움직이는소리8.mp3",
                "Assets/Audio/SFX/인형움직이는소리9.mp3"
            });
            Assign(values, "uiHovered", "Assets/Audio/SFX/UI_Hovered.wav");
            Assign(values, "uiClick", "Assets/Audio/SFX/UI_Click.wav");
            values.ApplyModifiedPropertiesWithoutUndo();

            foreach (string path in new[]
                     {
                         "Assets/Audio/SFX/키줍는소리.mp3",
                         "Assets/Audio/SFX/종이줍는소리.mp3",
                         "Assets/Audio/SFX/손전등드는소리_[cut_0sec].mp3",
                         "Assets/Audio/SFX/손전등_[cut_0sec].mp3",
                         "Assets/Audio/SFX/문 여는 소리_[cut_2sec].mp3",
                         "Assets/Audio/SFX/문이 쾅 닫는 소리_[cut_0sec].mp3",
                         "Assets/Audio/SFX/전등깜빡이는.mp3",
                         "Assets/Audio/SFX/첫번째출구여는소리.mp3",
                         "Assets/Audio/SFX/인형움직이는소리1.mp3",
                         "Assets/Audio/SFX/인형움직이는소리2.mp3",
                         "Assets/Audio/SFX/인형움직이는소리3.mp3",
                         "Assets/Audio/SFX/인형움직이는소리4.mp3",
                         "Assets/Audio/SFX/인형움직이는소리5.mp3",
                         "Assets/Audio/SFX/인형움직이는소리6.mp3",
                         "Assets/Audio/SFX/인형움직이는소리7.mp3",
                         "Assets/Audio/SFX/인형움직이는소리8.mp3",
                         "Assets/Audio/SFX/인형움직이는소리9.mp3"
                     })
            {
                ConfigureImporter(path, AudioClipLoadType.CompressedInMemory);
            }
            ConfigureImporter("Assets/Audio/SFX/UI_Hovered.wav", AudioClipLoadType.DecompressOnLoad);
            ConfigureImporter("Assets/Audio/SFX/UI_Click.wav", AudioClipLoadType.DecompressOnLoad);

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("GAME_SFX_LIBRARY_COMPLETE: all requested SFX clips connected.");
        }

        private static void Assign(SerializedObject values, string propertyName, string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new FileNotFoundException($"Missing SFX clip: {path}");
            }
            values.FindProperty(propertyName).objectReferenceValue = clip;
        }

        private static void AssignList(
            SerializedObject values, string propertyName, IReadOnlyList<string> paths)
        {
            SerializedProperty list = values.FindProperty(propertyName);
            list.arraySize = paths.Count;
            for (int index = 0; index < paths.Count; index++)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[index]);
                if (clip == null)
                {
                    throw new FileNotFoundException($"Missing SFX clip: {paths[index]}");
                }
                list.GetArrayElementAtIndex(index).objectReferenceValue = clip;
            }
        }

        private static void ConfigureImporter(string path, AudioClipLoadType loadType)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing SFX importer: {path}");
            }

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
