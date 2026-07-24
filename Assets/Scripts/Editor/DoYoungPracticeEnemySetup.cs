#if UNITY_EDITOR
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.Enemy;
using NHNHackathon.Game;
using NHNHackathon.Input;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class DoYoungPracticeEnemySetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Characters/Watcher.prefab";
        [MenuItem("Tools/NHN Hackathon/Rebuild DoYoung Practice Enemy")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("Watcher");
            DeleteNamedObject("WatcherPatrolRoute");
            DeleteNamedObject("GameOverSystem");

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new System.InvalidOperationException("DoYoungPracticeScene requires a Player object.");
            }

            PlayerDisguiseState disguise = GetOrAdd<PlayerDisguiseState>(player);
            LightStimulusSource flashlightStimulus = ConfigureFlashlight(player);
            GameOverController gameOver = CreateGameOverSystem(player, flashlightStimulus);
            EnemyPatrolRoute route = CreatePatrolRoute();
            GameObject watcher = CreateWatcher(player.transform, disguise, route, gameOver);

            EnsureDirectory("Assets/Prefabs/Characters");
            PrefabUtility.SaveAsPrefabAssetAndConnect(watcher, EnemyPrefabPath, InteractionMode.AutomatedAction);
            BakeNavMesh();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DoYoungPracticeScene enemy setup completed.");
        }

        private static LightStimulusSource ConfigureFlashlight(GameObject player)
        {
            LightStimulusSource existing = player.GetComponentInChildren<LightStimulusSource>(true);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Camera playerCamera = player.GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
            {
                throw new System.InvalidOperationException("Player requires a child Camera for the flashlight.");
            }
            GameObject flashlightObject = new GameObject("FlashlightStimulus");
            flashlightObject.transform.SetParent(playerCamera.transform, false);
            flashlightObject.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            flashlightObject.transform.localRotation = Quaternion.identity;
            Light flashlight = flashlightObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 12f;
            flashlight.spotAngle = 55f;
            flashlight.intensity = 8f;
            LightStimulusSource stimulus = flashlightObject.AddComponent<LightStimulusSource>();
            PlayerFlashlightController controller = flashlightObject.AddComponent<PlayerFlashlightController>();

            SerializedObject stimulusSettings = new SerializedObject(stimulus);
            stimulusSettings.FindProperty("linkedLight").objectReferenceValue = flashlight;
            stimulusSettings.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject controllerSettings = new SerializedObject(controller);
            controllerSettings.FindProperty("flashlight").objectReferenceValue = flashlight;
            controllerSettings.FindProperty("toggleKey").intValue = (int)KeyCode.F;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();
            return stimulus;
        }

        private static GameOverController CreateGameOverSystem(
            GameObject player, LightStimulusSource flashlightStimulus)
        {
            GameObject root = new GameObject("GameOverSystem");
            GameOverController controller = root.AddComponent<GameOverController>();
            Behaviour[] controls =
            {
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<PlayerCameraController>(),
                player.GetComponent<PlayerDanceInput>(),
                player.GetComponent<PlayerCursorController>(),
                flashlightStimulus.GetComponent<PlayerFlashlightController>()
            };
            SerializedObject settings = new SerializedObject(controller);
            SerializedProperty controlsProperty = settings.FindProperty("playerControls");
            controlsProperty.arraySize = controls.Length;
            for (int index = 0; index < controls.Length; index++)
            {
                controlsProperty.GetArrayElementAtIndex(index).objectReferenceValue = controls[index];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static EnemyPatrolRoute CreatePatrolRoute()
        {
            GameObject routeObject = new GameObject("WatcherPatrolRoute");
            EnemyPatrolRoute route = routeObject.AddComponent<EnemyPatrolRoute>();
            Vector3[] positions =
            {
                new Vector3(-7f, 0f, -7f), new Vector3(-7f, 0f, 7f),
                new Vector3(7f, 0f, 7f), new Vector3(7f, 0f, -7f)
            };
            SerializedObject routeSettings = new SerializedObject(route);
            SerializedProperty points = routeSettings.FindProperty("points");
            points.arraySize = positions.Length;
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject point = new GameObject($"PatrolPoint_{index + 1}");
                point.transform.SetParent(routeObject.transform);
                point.transform.position = positions[index];
                points.GetArrayElementAtIndex(index).objectReferenceValue = point.transform;
            }
            routeSettings.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static GameObject CreateWatcher(
            Transform player, PlayerDisguiseState disguise, EnemyPatrolRoute route, GameOverController gameOver)
        {
            GameObject root = new GameObject("Watcher");
            root.layer = 2;
            root.transform.position = new Vector3(-7f, 1f, -7f);
            root.AddComponent<CapsuleCollider>();
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            EnemyPerception perception = root.AddComponent<EnemyPerception>();
            EnemyController controller = root.AddComponent<EnemyController>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.layer = 2;
            visual.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateWatcherMaterial();

            SerializedObject perceptionSettings = new SerializedObject(perception);
            perceptionSettings.FindProperty("obstructionMask").intValue = 1 << 0;
            perceptionSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("player").objectReferenceValue = player;
            settings.FindProperty("playerDisguise").objectReferenceValue = disguise;
            settings.FindProperty("patrolRoute").objectReferenceValue = route;
            settings.FindProperty("gameOverController").objectReferenceValue = gameOver;
            settings.FindProperty("enemyRenderer").objectReferenceValue = renderer;
            settings.FindProperty("lostSightGraceDuration").floatValue = 2f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static Material CreateWatcherMaterial()
        {
            const string path = "Assets/Art/Materials/WatcherPrototype.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = new Color(0.12f, 0.12f, 0.16f) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void BakeNavMesh()
        {
            GameObject environment = GameObject.Find("PlayerMovementEnvironment");
            if (environment != null)
            {
                foreach (Transform child in environment.GetComponentsInChildren<Transform>())
                {
                    GameObjectUtility.SetStaticEditorFlags(
                        child.gameObject, StaticEditorFlags.NavigationStatic);
                }
            }
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
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
            if (AssetDatabase.IsValidFolder(directory)) return;
            string parent = System.IO.Path.GetDirectoryName(directory)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(directory));
        }
    }
}
#endif
