#if UNITY_EDITOR
using NHNHackathon.AI;
using NHNHackathon.Dance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1DanceSyncSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string CatalogPath = "Assets/ScriptableObjects/Dance/DanceCatalog.asset";
        private const string MappingPath = "Assets/ScriptableObjects/Dance/DanceInputMapping.asset";
        private const string DancingAIPrefabPath = "Assets/Prefabs/Characters/DancingAI.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player.prefab";
        private const string ZonePrefabPath = "Assets/Prefabs/Interactables/DanceSyncZone.prefab";
        [MenuItem("Tools/NHN Hackathon/Rebuild Level1 Dance Sync")]
        public static void Build()
        {
            DanceCatalog catalog = CreateOrUpdateCatalog();
            DanceInputMapping mapping = CreateOrUpdateInputMapping();
            UpdatePlayerPrefab(catalog, mapping);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("DancingAI");
            DeleteNamedObject("DanceSyncZone");
            GameObject dancingAI = CreateDancingAI(catalog);
            GameObject zone = CreateDanceZone(dancingAI.GetComponent<DanceSequenceController>());

            EnsureDirectory("Assets/Prefabs/Characters");
            EnsureDirectory("Assets/Prefabs/Interactables");
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                dancingAI, DancingAIPrefabPath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(zone, ZonePrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Level1 dance sync setup completed.");
        }

        private static DanceCatalog CreateOrUpdateCatalog()
        {
            EnsureDirectory("Assets/ScriptableObjects/Dance");
            DanceCatalog catalog = AssetDatabase.LoadAssetAtPath<DanceCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DanceCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty dances = serializedCatalog.FindProperty("dances");
            dances.arraySize = 6;
            Color[] colors = { Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.cyan };
            for (int index = 0; index < dances.arraySize; index++)
            {
                SerializedProperty dance = dances.GetArrayElementAtIndex(index);
                dance.FindPropertyRelative("id").intValue = index + 1;
                dance.FindPropertyRelative("danceName").stringValue = $"Dance {index + 1}";
                dance.FindPropertyRelative("displayColor").colorValue = colors[index];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static DanceInputMapping CreateOrUpdateInputMapping()
        {
            EnsureDirectory("Assets/ScriptableObjects/Dance");
            DanceInputMapping mapping = AssetDatabase.LoadAssetAtPath<DanceInputMapping>(MappingPath);
            if (mapping == null)
            {
                mapping = ScriptableObject.CreateInstance<DanceInputMapping>();
                AssetDatabase.CreateAsset(mapping, MappingPath);
            }

            SerializedObject serializedMapping = new SerializedObject(mapping);
            SerializedProperty bindings = serializedMapping.FindProperty("bindings");
            bindings.arraySize = 6;
            for (int index = 0; index < bindings.arraySize; index++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
                binding.FindPropertyRelative("key").intValue = (int)KeyCode.Alpha1 + index;
                binding.FindPropertyRelative("danceId").intValue = index + 1;
            }

            serializedMapping.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mapping);
            return mapping;
        }

        private static void UpdatePlayerPrefab(DanceCatalog catalog, DanceInputMapping mapping)
        {
            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Renderer renderer = playerRoot.GetComponentInChildren<Renderer>();
                DanceColorVisualizer visualizer =
                    playerRoot.GetComponent<DanceColorVisualizer>() ?? playerRoot.AddComponent<DanceColorVisualizer>();
                PlayerDanceInput input =
                    playerRoot.GetComponent<PlayerDanceInput>() ?? playerRoot.AddComponent<PlayerDanceInput>();

                SerializedObject visualizerSettings = new SerializedObject(visualizer);
                visualizerSettings.FindProperty("danceCatalog").objectReferenceValue = catalog;
                visualizerSettings.FindProperty("targetRenderer").objectReferenceValue = renderer;
                visualizerSettings.FindProperty("defaultColor").colorValue = new Color(0.65f, 0.68f, 0.72f);
                visualizerSettings.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject inputSettings = new SerializedObject(input);
                inputSettings.FindProperty("inputMapping").objectReferenceValue = mapping;
                inputSettings.FindProperty("displayDuration").floatValue = 0.5f;
                inputSettings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }
        }

        private static GameObject CreateDancingAI(DanceCatalog catalog)
        {
            GameObject root = new GameObject("DancingAI");
            root.transform.position = new Vector3(4f, 1f, 0f);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(
                "Assets/Art/Materials/DancingAIPrototype.mat", Color.white);

            DanceSequenceController controller = root.AddComponent<DanceSequenceController>();
            DanceColorVisualizer visualizer = root.AddComponent<DanceColorVisualizer>();
            root.AddComponent<AIDanceColorPresenter>();
            SerializedObject controllerSettings = new SerializedObject(controller);
            controllerSettings.FindProperty("danceCatalog").objectReferenceValue = catalog;
            controllerSettings.FindProperty("beatInterval").floatValue = 1f;
            SerializedProperty sequence = controllerSettings.FindProperty("danceSequence");
            sequence.arraySize = 3;
            sequence.GetArrayElementAtIndex(0).intValue = 1;
            sequence.GetArrayElementAtIndex(1).intValue = 2;
            sequence.GetArrayElementAtIndex(2).intValue = 3;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject visualizerSettings = new SerializedObject(visualizer);
            visualizerSettings.FindProperty("danceCatalog").objectReferenceValue = catalog;
            visualizerSettings.FindProperty("targetRenderer").objectReferenceValue = renderer;
            visualizerSettings.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject CreateDanceZone(DanceSequenceController danceAI)
        {
            GameObject zone = new GameObject("DanceSyncZone");
            zone.transform.position = new Vector3(4f, 1f, 0f);
            BoxCollider collider = zone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(6f, 2.5f, 6f);
            DanceSyncJudge judge = zone.AddComponent<DanceSyncJudge>();
            zone.AddComponent<DanceSyncZone>();
            SerializedObject judgeSettings = new SerializedObject(judge);
            judgeSettings.FindProperty("danceAI").objectReferenceValue = danceAI;
            judgeSettings.FindProperty("timingTolerance").floatValue = 0.5f;
            judgeSettings.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            EnsureDirectory("Assets/Art/Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void DeleteNamedObject(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void EnsureDirectory(string directory)
        {
            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(directory)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(directory);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
