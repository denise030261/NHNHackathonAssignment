using OnlyOnePlayer.Prototype.Characters;
using OnlyOnePlayer.Prototype.Core;
using OnlyOnePlayer.Prototype.Input;
using OnlyOnePlayer.Prototype.Mission;
using OnlyOnePlayer.Prototype.NPC;
using OnlyOnePlayer.Prototype.Obstacles;
using OnlyOnePlayer.Prototype.Stealth;
using OnlyOnePlayer.Prototype.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Editor
{
    public static class OnlyOnePlayerSceneBuilder
    {
        private const string ScenePath = "Assets/Prototype/OnlyOnePlayer/Scenes/OnlyOnePllayerScene.unity";
        private const float DefaultCharacterMoveSpeed = 3f;
        private const float Slow80SpeedMultiplier = 0.2f;

        [MenuItem("Prototype/Only One Player/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);

            ClearPrototypeObjects();

            PlayerInputReader inputReader = CreateInputReader();
            ConfigureMainCamera();
            GameObject player = CreateCharacter("Player_Real", new Vector2(0f, 0f), Color.green, inputReader, null);
            RealPlayerIdentity realPlayer = player.AddComponent<RealPlayerIdentity>();
            CreateCharacter("NPC_Same", new Vector2(-2f, 1.5f), Color.cyan, inputReader, NpcFollowType.Same);
            CreateCharacter("NPC_InvertW", new Vector2(2f, 1.5f), Color.red, inputReader, NpcFollowType.InvertW);
            CreateCharacter("NPC_InvertA", new Vector2(-2f, -1.5f), new Color(1f, 0.55f, 0f), inputReader, NpcFollowType.InvertA);
            CreateCharacter("NPC_InvertAll", new Vector2(2f, -1.5f), Color.magenta, inputReader, NpcFollowType.InvertAll);
            CreateCharacter("NPC_Delayed", new Vector2(0f, 2.8f), Color.yellow, inputReader, NpcFollowType.Delayed);
            CreateCharacter("NPC_Slow80", new Vector2(0f, -2.8f), new Color(0.55f, 0.75f, 1f), inputReader, NpcFollowType.Slow80);
            CreateCharacter("NPC_Ignore10", new Vector2(3f, 0f), new Color(0.8f, 0.8f, 0.8f), inputReader, NpcFollowType.Ignore10);
            WatcherController2D watcher = CreateWatcher(realPlayer);
            CreateObstacleSamples();
            CreateOneWayZoneSamples();
            CreateCheckpointSamples();
            CreateMission(realPlayer, watcher);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ClearPrototypeObjects()
        {
            foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (gameObject.name.StartsWith("Player_", System.StringComparison.Ordinal) ||
                    gameObject.name.StartsWith("NPC_", System.StringComparison.Ordinal) ||
                    gameObject.name.StartsWith("WatcherPatrolPoint_", System.StringComparison.Ordinal) ||
                    gameObject.name == "PlayerInputManager" ||
                    gameObject.name == "GameManager" ||
                    gameObject.name == "Watcher" ||
                    gameObject.name == "NPCSpawner" ||
                    gameObject.name == "Obstacles_Root" ||
                    gameObject.name == "Gimmicks_Root" ||
                    gameObject.name == "OneWayZones_Root" ||
                    gameObject.name == "Checkpoint_Root" ||
                    gameObject.name == "MissionManager" ||
                    gameObject.name == "DataExtractionZone" ||
                    gameObject.name == "Exit_Root")
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        private static PlayerInputReader CreateInputReader()
        {
            var gameObject = new GameObject("PlayerInputManager");
            return gameObject.AddComponent<PlayerInputReader>();
        }

        private static void ConfigureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        }

        private static GameObject CreateCharacter(
            string name,
            Vector2 position,
            Color color,
            PlayerInputReader inputReader,
            NpcFollowType? npcFollowType)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;

            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            var visual = gameObject.AddComponent<PrototypeCharacterVisual>();
            var body = gameObject.AddComponent<Rigidbody2D>();
            var collider = gameObject.AddComponent<BoxCollider2D>();
            var identity = gameObject.AddComponent<CharacterIdentity>();
            gameObject.AddComponent<CharacterStatus>();
            var mover = gameObject.AddComponent<CharacterMover2D>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            collider.size = Vector2.one;
            identity.Configure(npcFollowType.HasValue ? CharacterActorType.Npc : CharacterActorType.RealPlayer, name);

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("characterColor").colorValue = color;
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            float moveSpeed = npcFollowType == NpcFollowType.Slow80
                ? DefaultCharacterMoveSpeed * Slow80SpeedMultiplier
                : DefaultCharacterMoveSpeed;

            SerializedObject moverObject = new SerializedObject(mover);
            moverObject.FindProperty("moveSpeed").floatValue = moveSpeed;
            moverObject.FindProperty("targetRigidbody").objectReferenceValue = body;
            moverObject.ApplyModifiedPropertiesWithoutUndo();

            if (npcFollowType.HasValue)
            {
                var follower = gameObject.AddComponent<NpcInputFollower>();
                SerializedObject followerObject = new SerializedObject(follower);
                followerObject.FindProperty("inputReader").objectReferenceValue = inputReader;
                followerObject.FindProperty("mover").objectReferenceValue = mover;
                followerObject.FindProperty("followType").enumValueIndex = (int)npcFollowType.Value;
                followerObject.FindProperty("inputDelay").floatValue = 0.5f;
                followerObject.FindProperty("ignoreInputChance").floatValue = 0.1f;
                followerObject.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var controller = gameObject.AddComponent<PlayerCharacterController>();
                SerializedObject controllerObject = new SerializedObject(controller);
                controllerObject.FindProperty("inputReader").objectReferenceValue = inputReader;
                controllerObject.FindProperty("mover").objectReferenceValue = mover;
                controllerObject.ApplyModifiedPropertiesWithoutUndo();
            }

            return gameObject;
        }

        private static WatcherController2D CreateWatcher(RealPlayerIdentity playerTarget)
        {
            var gameManager = new GameObject("GameManager");
            var gameOverHandler = gameManager.AddComponent<PrototypeGameOverHandler>();
            var reporter = gameManager.AddComponent<RuleViolationReporter>();
            var forbiddenReporter = gameManager.AddComponent<ForbiddenActionReporter>();
            var broadcastSystem = gameManager.AddComponent<BroadcastSystemController>();
            var broadcastMonitor = gameManager.AddComponent<BroadcastRuleMonitor2D>();

            var patrolPointA = new GameObject("WatcherPatrolPoint_A");
            patrolPointA.transform.position = new Vector2(-4f, 0f);

            var patrolPointB = new GameObject("WatcherPatrolPoint_B");
            patrolPointB.transform.position = new Vector2(4f, 0f);

            var watcherObject = new GameObject("Watcher");
            watcherObject.transform.position = patrolPointA.transform.position;

            var spriteRenderer = watcherObject.AddComponent<SpriteRenderer>();
            var visual = watcherObject.AddComponent<PrototypeCharacterVisual>();
            var body = watcherObject.AddComponent<Rigidbody2D>();
            var identity = watcherObject.AddComponent<CharacterIdentity>();
            watcherObject.AddComponent<CharacterStatus>();
            var watcher = watcherObject.AddComponent<WatcherController2D>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            identity.Configure(CharacterActorType.Watcher, "Watcher");

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("characterColor").colorValue = new Color(1f, 0.15f, 0.05f);
            visualObject.FindProperty("size").floatValue = 1f;
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject watcherObjectData = new SerializedObject(watcher);
            watcherObjectData.FindProperty("targetRigidbody").objectReferenceValue = body;
            watcherObjectData.FindProperty("patrolPointA").objectReferenceValue = patrolPointA.transform;
            watcherObjectData.FindProperty("patrolPointB").objectReferenceValue = patrolPointB.transform;
            watcherObjectData.FindProperty("gameOverHandler").objectReferenceValue = gameOverHandler;
            watcherObjectData.FindProperty("realPlayerTarget").objectReferenceValue = playerTarget;
            watcherObjectData.FindProperty("testChaseTarget").objectReferenceValue = playerTarget;
            watcherObjectData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject reporterObjectData = new SerializedObject(reporter);
            reporterObjectData.FindProperty("watchers").arraySize = 1;
            reporterObjectData.FindProperty("watchers").GetArrayElementAtIndex(0).objectReferenceValue = watcher;
            reporterObjectData.FindProperty("realPlayer").objectReferenceValue = playerTarget;
            reporterObjectData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject forbiddenReporterData = new SerializedObject(forbiddenReporter);
            forbiddenReporterData.FindProperty("watchers").arraySize = 1;
            forbiddenReporterData.FindProperty("watchers").GetArrayElementAtIndex(0).objectReferenceValue = watcher;
            forbiddenReporterData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject broadcastSystemData = new SerializedObject(broadcastSystem);
            broadcastSystemData.FindProperty("showBroadcastUi").boolValue = true;
            broadcastSystemData.FindProperty("fontSize").intValue = 28;
            broadcastSystemData.FindProperty("textColor").colorValue = Color.white;
            broadcastSystemData.FindProperty("backgroundColor").colorValue = new Color(0f, 0f, 0f, 0.65f);
            broadcastSystemData.FindProperty("playOnStart").boolValue = true;
            broadcastSystemData.FindProperty("startDelaySeconds").floatValue = 2f;
            broadcastSystemData.FindProperty("staticMessageSeconds").floatValue = 1.25f;
            broadcastSystemData.FindProperty("instructionSeconds").floatValue = 4f;
            broadcastSystemData.FindProperty("endMessageSeconds").floatValue = 1.25f;
            broadcastSystemData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject broadcastMonitorData = new SerializedObject(broadcastMonitor);
            broadcastMonitorData.FindProperty("broadcastSystem").objectReferenceValue = broadcastSystem;
            broadcastMonitorData.FindProperty("reporter").objectReferenceValue = forbiddenReporter;
            broadcastMonitorData.FindProperty("minimumMoveDistance").floatValue = 0.02f;
            broadcastMonitorData.FindProperty("reportCooldownSeconds").floatValue = 1f;
            broadcastMonitorData.ApplyModifiedPropertiesWithoutUndo();

            return watcher;
        }

        private static void CreateObstacleSamples()
        {
            var obstaclesRoot = new GameObject("Obstacles_Root");
            CreateBlockingObstacle("BlockingObstacle_Center", obstaclesRoot.transform, new Vector2(0f, -2f), new Vector2(2.5f, 0.5f));
            CreateBlockingObstacle("BlockingObstacle_Right", obstaclesRoot.transform, new Vector2(3f, 1.2f), new Vector2(0.6f, 2f));

            var gimmicksRoot = new GameObject("Gimmicks_Root");
            CreateStunGimmick("StunGimmick_TestPad", gimmicksRoot.transform, new Vector2(-3f, -1.8f), new Vector2(1.4f, 1.4f));
        }

        private static void CreateBlockingObstacle(string name, Transform root, Vector2 position, Vector2 size)
        {
            var obstacle = new GameObject(name);
            obstacle.transform.SetParent(root);
            obstacle.transform.position = position;

            var spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
            var boxCollider = obstacle.AddComponent<BoxCollider2D>();
            var visual = obstacle.AddComponent<PrototypeObstacleVisual>();
            var blocking = obstacle.AddComponent<BlockingObstacle2D>();

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("obstacleColor").colorValue = new Color(0.45f, 0.48f, 0.52f);
            visualObject.FindProperty("size").vector2Value = size;
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject blockingObject = new SerializedObject(blocking);
            blockingObject.FindProperty("obstacleCollider").objectReferenceValue = boxCollider;
            blockingObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateStunGimmick(string name, Transform root, Vector2 position, Vector2 size)
        {
            var gimmick = new GameObject(name);
            gimmick.transform.SetParent(root);
            gimmick.transform.position = position;

            var spriteRenderer = gimmick.AddComponent<SpriteRenderer>();
            var boxCollider = gimmick.AddComponent<BoxCollider2D>();
            var visual = gimmick.AddComponent<PrototypeObstacleVisual>();
            var stunGimmick = gimmick.AddComponent<StunGimmick2D>();

            boxCollider.isTrigger = true;

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("obstacleColor").colorValue = new Color(0.25f, 0.85f, 1f, 0.8f);
            visualObject.FindProperty("size").vector2Value = size;
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject stunObject = new SerializedObject(stunGimmick);
            stunObject.FindProperty("gimmickCollider").objectReferenceValue = boxCollider;
            stunObject.FindProperty("stunDuration").floatValue = 1.5f;
            stunObject.FindProperty("useTriggerCollider").boolValue = true;
            stunObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateOneWayZoneSamples()
        {
            var zonesRoot = new GameObject("OneWayZones_Root");
            CreateOneWayZone(
                "OneWayZone_Right",
                zonesRoot.transform,
                new Vector2(-1.2f, 0.8f),
                new Vector2(2.6f, 1.1f),
                OneWayDirection2D.Right);
        }

        private static void CreateOneWayZone(
            string name,
            Transform root,
            Vector2 position,
            Vector2 size,
            OneWayDirection2D allowedDirection)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(root);
            zone.transform.position = position;

            var spriteRenderer = zone.AddComponent<SpriteRenderer>();
            var boxCollider = zone.AddComponent<BoxCollider2D>();
            var oneWayZone = zone.AddComponent<OneWayZone2D>();
            var visual = zone.AddComponent<OneWayZoneVisual2D>();

            boxCollider.isTrigger = true;

            SerializedObject zoneObject = new SerializedObject(oneWayZone);
            zoneObject.FindProperty("reporter").objectReferenceValue = Object.FindAnyObjectByType<ForbiddenActionReporter>();
            zoneObject.FindProperty("triggerCollider").objectReferenceValue = boxCollider;
            zoneObject.FindProperty("allowedDirection").enumValueIndex = (int)allowedDirection;
            zoneObject.FindProperty("violationDotThreshold").floatValue = -0.2f;
            zoneObject.FindProperty("minimumMoveDistance").floatValue = 0.015f;
            zoneObject.FindProperty("reportCooldownSeconds").floatValue = 1f;
            zoneObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("oneWayZone").objectReferenceValue = oneWayZone;
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("zoneColor").colorValue = new Color(0.25f, 0.9f, 0.45f, 0.35f);
            visualObject.FindProperty("arrowColor").colorValue = new Color(0.05f, 1f, 0.25f, 1f);
            visualObject.FindProperty("size").vector2Value = size;
            visualObject.FindProperty("arrowWidth").floatValue = 0.08f;
            visualObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCheckpointSamples()
        {
            var checkpointRoot = new GameObject("Checkpoint_Root");
            var accessController = checkpointRoot.AddComponent<CheckpointAccessController2D>();

            SerializedObject accessObject = new SerializedObject(accessController);
            accessObject.FindProperty("consumePermissionOnPass").boolValue = true;
            accessObject.ApplyModifiedPropertiesWithoutUndo();

            CreateCheckpointWaitZone(
                "Checkpoint_WaitZone",
                checkpointRoot.transform,
                new Vector2(1.8f, -0.8f),
                new Vector2(1.4f, 1.1f),
                accessController);

            CreateCheckpointGate(
                "Checkpoint_Gate",
                checkpointRoot.transform,
                new Vector2(3.1f, -0.8f),
                new Vector2(0.35f, 1.8f),
                accessController);
        }

        private static void CreateCheckpointWaitZone(
            string name,
            Transform root,
            Vector2 position,
            Vector2 size,
            CheckpointAccessController2D accessController)
        {
            var waitZone = new GameObject(name);
            waitZone.transform.SetParent(root);
            waitZone.transform.position = position;

            var spriteRenderer = waitZone.AddComponent<SpriteRenderer>();
            var boxCollider = waitZone.AddComponent<BoxCollider2D>();
            var visual = waitZone.AddComponent<PrototypeObstacleVisual>();
            var checkpointWaitZone = waitZone.AddComponent<CheckpointWaitZone2D>();

            boxCollider.isTrigger = true;

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("obstacleColor").colorValue = new Color(1f, 0.85f, 0.2f, 0.45f);
            visualObject.FindProperty("size").vector2Value = size;
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject waitZoneObject = new SerializedObject(checkpointWaitZone);
            waitZoneObject.FindProperty("accessController").objectReferenceValue = accessController;
            waitZoneObject.FindProperty("triggerCollider").objectReferenceValue = boxCollider;
            waitZoneObject.FindProperty("requiredWaitSeconds").floatValue = 3f;
            waitZoneObject.FindProperty("resetProgressWhenLeaving").boolValue = true;
            waitZoneObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCheckpointGate(
            string name,
            Transform root,
            Vector2 position,
            Vector2 size,
            CheckpointAccessController2D accessController)
        {
            var gate = new GameObject(name);
            gate.transform.SetParent(root);
            gate.transform.position = position;

            var spriteRenderer = gate.AddComponent<SpriteRenderer>();
            var boxCollider = gate.AddComponent<BoxCollider2D>();
            var visual = gate.AddComponent<PrototypeObstacleVisual>();
            var checkpointGate = gate.AddComponent<CheckpointGate2D>();

            boxCollider.isTrigger = true;

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("obstacleColor").colorValue = new Color(1f, 0.45f, 0.05f, 0.8f);
            visualObject.FindProperty("size").vector2Value = size;
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject gateObject = new SerializedObject(checkpointGate);
            gateObject.FindProperty("accessController").objectReferenceValue = accessController;
            gateObject.FindProperty("reporter").objectReferenceValue = Object.FindAnyObjectByType<ForbiddenActionReporter>();
            gateObject.FindProperty("triggerCollider").objectReferenceValue = boxCollider;
            gateObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMission(RealPlayerIdentity realPlayer, WatcherController2D watcher)
        {
            var missionManager = new GameObject("MissionManager");
            var missionController = missionManager.AddComponent<ConfidentialDataMissionController>();

            SerializedObject missionObject = new SerializedObject(missionController);
            missionObject.FindProperty("realPlayer").objectReferenceValue = realPlayer;
            missionObject.FindProperty("watchers").arraySize = 1;
            missionObject.FindProperty("watchers").GetArrayElementAtIndex(0).objectReferenceValue = watcher;
            missionObject.FindProperty("requiredDataCount").intValue = 2;
            missionObject.ApplyModifiedPropertiesWithoutUndo();

            var zone = new GameObject("DataExtractionZone");
            zone.transform.position = new Vector2(-1.5f, 2.8f);
            zone.AddComponent<DataExtractionZone2D>();

            CreateDataPoint("DataPoint_A", zone.transform, new Vector2(-2.4f, 2.8f), missionController);
            CreateDataPoint("DataPoint_B", zone.transform, new Vector2(-0.6f, 2.8f), missionController);

            var exitRoot = new GameObject("Exit_Root");
            CreateExit("Exit_Real", exitRoot.transform, new Vector2(4.2f, -3.4f), ExitType.Real, missionController, new Color(0.2f, 1f, 0.35f, 0.85f));
            CreateExit("Exit_Fake_Left", exitRoot.transform, new Vector2(-4.2f, -3.4f), ExitType.Fake, missionController, new Color(1f, 0.35f, 0.1f, 0.85f));
            CreateExit("Exit_Fake_Top", exitRoot.transform, new Vector2(4.2f, 3.4f), ExitType.Fake, missionController, new Color(1f, 0.35f, 0.1f, 0.85f));
        }

        private static void CreateDataPoint(string name, Transform root, Vector2 position, ConfidentialDataMissionController missionController)
        {
            var point = new GameObject(name);
            point.transform.SetParent(root);
            point.transform.position = position;

            var spriteRenderer = point.AddComponent<SpriteRenderer>();
            var boxCollider = point.AddComponent<BoxCollider2D>();
            var visual = point.AddComponent<PrototypeObstacleVisual>();
            var dataPoint = point.AddComponent<DataExtractionPoint2D>();
            var tracker = point.AddComponent<TerminalForbiddenActionTracker2D>();

            boxCollider.isTrigger = true;

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("obstacleColor").colorValue = new Color(0.1f, 0.7f, 1f, 0.75f);
            visualObject.FindProperty("size").vector2Value = new Vector2(0.9f, 0.9f);
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject pointObject = new SerializedObject(dataPoint);
            pointObject.FindProperty("missionController").objectReferenceValue = missionController;
            pointObject.FindProperty("requiredStaySeconds").floatValue = 3f;
            pointObject.FindProperty("resetProgressWhenPlayerLeaves").boolValue = true;
            pointObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject trackerObject = new SerializedObject(tracker);
            trackerObject.FindProperty("reporter").objectReferenceValue = Object.FindAnyObjectByType<ForbiddenActionReporter>();
            trackerObject.FindProperty("dataPoint").objectReferenceValue = dataPoint;
            trackerObject.FindProperty("maxStaySeconds").floatValue = 5f;
            trackerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateExit(
            string name,
            Transform root,
            Vector2 position,
            ExitType exitType,
            ConfidentialDataMissionController missionController,
            Color color)
        {
            var exit = new GameObject(name);
            exit.transform.SetParent(root);
            exit.transform.position = position;

            var spriteRenderer = exit.AddComponent<SpriteRenderer>();
            var boxCollider = exit.AddComponent<BoxCollider2D>();
            var visual = exit.AddComponent<PrototypeObstacleVisual>();
            var exitZone = exit.AddComponent<ExitZone2D>();

            boxCollider.isTrigger = true;

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            visualObject.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            visualObject.FindProperty("obstacleColor").colorValue = color;
            visualObject.FindProperty("size").vector2Value = new Vector2(1.2f, 0.7f);
            visualObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject exitObject = new SerializedObject(exitZone);
            exitObject.FindProperty("missionController").objectReferenceValue = missionController;
            exitObject.FindProperty("forbiddenActionReporter").objectReferenceValue = Object.FindAnyObjectByType<ForbiddenActionReporter>();
            exitObject.FindProperty("exitType").enumValueIndex = (int)exitType;
            exitObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
