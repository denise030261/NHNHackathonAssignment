#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Characters;
using NHNHackathon.Cinematics;
using NHNHackathon.Dance;
using NHNHackathon.Enemy;
using NHNHackathon.ExitSystem;
using NHNHackathon.Interaction;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1Zone1WatcherCutsceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string DancerPrefabPath = "Assets/Prefabs/Characters/DancingAI.prefab";

        [MenuItem("NHN Hackathon/Setup/Level1 Zone1 Watcher Cutscene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject zone1 = scene.GetRootGameObjects().First(root => root.name == "Zone1");
            GameObject zone12 = scene.GetRootGameObjects().First(root => root.name == "Zone1_2");
            ExitDoor door = zone1.GetComponentsInChildren<ExitDoor>(true).First(value => value.name.Trim() == "ExitDoor");
            EnemyController watcher = zone12.GetComponentsInChildren<EnemyController>(true).First();
            PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            Camera playerCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

            Transform oldRoot = zone12.transform.Find("Zone1WatcherCutscene");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot.gameObject);
            GameObject root = new GameObject("Zone1WatcherCutscene");
            root.transform.SetParent(zone12.transform, true);
            root.transform.position = Vector3.zero;
            ZoneWatcherCaptureCutscene director = root.AddComponent<ZoneWatcherCaptureCutscene>();

            Transform dancerPoint = CreatePoint(root.transform, "FailedDancerPoint",
                watcher.transform.position + watcher.transform.forward * 4f);
            Transform capturePoint = CreatePoint(root.transform, "WatcherCapturePoint",
                dancerPoint.position - watcher.transform.forward * 1.1f);
            Transform lookTarget = CreatePoint(root.transform, "CameraLookTarget",
                dancerPoint.position + Vector3.up * 1.25f);
            Transform cameraPoint = CreatePoint(root.transform, "CutsceneCameraPoint",
                dancerPoint.position + watcher.transform.right * 4f + Vector3.up * 2f);
            Transform cameraRouteRoot = CreatePoint(root.transform, "CameraRoute", Vector3.zero);
            Transform[] cameraRoute = BuildCameraRoute(
                cameraRouteRoot, door.transform.position, cameraPoint.position);
            if (cameraRoute.Length > 0)
            {
                cameraPoint.position = cameraRoute[^1].position;
                Object.DestroyImmediate(cameraRoute[^1].gameObject);
                cameraRoute = cameraRoute.Take(cameraRoute.Length - 1).ToArray();
            }
            int blockedCameraSegments = CountBlockedSegments(cameraRoute, cameraPoint, 0.18f);
            Transform routeRoot = CreatePoint(root.transform, "CorridorRoute", watcher.transform.position);
            Transform route1 = CreatePoint(routeRoot, "Point_01", watcher.transform.position - watcher.transform.forward * 2.5f);
            Transform route2 = CreatePoint(routeRoot, "Point_02", watcher.transform.position - watcher.transform.forward * 5f);

            GameObject dancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DancerPrefabPath);
            GameObject dancer = (GameObject)PrefabUtility.InstantiatePrefab(dancerPrefab, scene);
            dancer.name = "FailedDanceDoll";
            dancer.transform.SetParent(root.transform, true);
            dancer.transform.SetPositionAndRotation(dancerPoint.position,
                Quaternion.LookRotation(-watcher.transform.forward, Vector3.up));
            dancer.SetActive(false);

            Behaviour[] controls = player.GetComponents<Behaviour>()
                .Where(value => value is PlayerMovement
                    || value is PlayerDanceInput
                    || value is PlayerInteractor)
                .Concat(player.GetComponentsInChildren<PlayerFlashlightController>(true))
                .ToArray();
            WatcherCapturePresenter presenter = watcher.GetComponentInChildren<WatcherCapturePresenter>(true);
            Animator watcherAnimator = watcher.GetComponentInChildren<Animator>(true);

            SerializedObject values = new SerializedObject(director);
            values.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            values.FindProperty("playerCameraController").objectReferenceValue = player.GetComponent<PlayerCameraController>();
            values.FindProperty("playerFlashlight").objectReferenceValue =
                player.GetComponentInChildren<PlayerFlashlightController>(true);
            SetArray(values.FindProperty("playerControls"), controls.Cast<Object>().ToArray());
            values.FindProperty("failedDancer").objectReferenceValue = dancer;
            values.FindProperty("failedDancerAnimator").objectReferenceValue = dancer.GetComponentInChildren<Animator>(true);
            values.FindProperty("watcherController").objectReferenceValue = watcher;
            values.FindProperty("watcherAgent").objectReferenceValue = watcher.GetComponent<NavMeshAgent>();
            values.FindProperty("watcherAnimator").objectReferenceValue = watcherAnimator;
            values.FindProperty("capturePresenter").objectReferenceValue = presenter;
            values.FindProperty("cutsceneCameraPoint").objectReferenceValue = cameraPoint;
            values.FindProperty("cameraLookTarget").objectReferenceValue = lookTarget;
            SetArray(values.FindProperty("cameraRoute"), cameraRoute.Cast<Object>().ToArray());
            values.FindProperty("watcherCapturePoint").objectReferenceValue = capturePoint;
            SetArray(values.FindProperty("corridorRoute"), new Object[] { route1, route2 });
            values.ApplyModifiedPropertiesWithoutUndo();

            Transform oldTrigger = door.transform.Find("Zone1WatcherCutsceneTrigger");
            if (oldTrigger != null) Object.DestroyImmediate(oldTrigger.gameObject);
            GameObject triggerObject = new GameObject("Zone1WatcherCutsceneTrigger");
            triggerObject.transform.SetParent(door.transform, false);
            triggerObject.layer = door.gameObject.layer;
            BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(4f, 2.2f, 3f);
            DoorPassageCutsceneTrigger passage = triggerObject.AddComponent<DoorPassageCutsceneTrigger>();
            SerializedObject triggerValues = new SerializedObject(passage);
            triggerValues.FindProperty("crossingAxis").objectReferenceValue = door.transform;
            triggerValues.FindProperty("cutscene").objectReferenceValue = director;
            triggerValues.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"ZONE1_WATCHER_CUTSCENE_COMPLETE Watcher={watcher.name}, Door={door.name}, Controls={controls.Length}, CameraPoints={cameraRoute.Length}, BlockedSegments={blockedCameraSegments}");
        }

        private static Transform CreatePoint(Transform parent, string name, Vector3 position)
        {
            Transform value = new GameObject(name).transform;
            value.SetParent(parent, true);
            value.position = position;
            return value;
        }

        private static Transform[] BuildCameraRoute(
            Transform parent, Vector3 startPosition, Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            Vector3 start = startPosition;
            Vector3 end = destination;
            bool hasStart = NavMesh.SamplePosition(startPosition, out NavMeshHit startHit, 4f, NavMesh.AllAreas);
            bool hasEnd = NavMesh.SamplePosition(destination, out NavMeshHit endHit, 4f, NavMesh.AllAreas);
            if (hasStart) start = startHit.position;
            if (hasEnd) end = endHit.position;

            Vector3[] corners = hasStart && hasEnd && NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path)
                && path.corners.Length > 1
                ? path.corners
                : new[] { startPosition, Vector3.Lerp(startPosition, destination, 0.5f), destination };

            return corners.Select((corner, index) =>
            {
                Vector3 cameraHeightPosition = corner + Vector3.up * 1.65f;
                return CreatePoint(parent, $"Point_{index + 1:00}", cameraHeightPosition);
            }).ToArray();
        }

        private static int CountBlockedSegments(
            Transform[] points, Transform finalPoint, float radius)
        {
            int blocked = 0;
            for (int index = 1; index < points.Length; index++)
                if (IsBlocked(points[index - 1].position, points[index].position, radius)) blocked++;
            if (points.Length > 0 && IsBlocked(points[^1].position, finalPoint.position, radius)) blocked++;
            return blocked;
        }

        private static bool IsBlocked(Vector3 from, Vector3 to, float radius)
        {
            Vector3 offset = to - from;
            return offset.sqrMagnitude > 0.001f && Physics.SphereCast(
                from, radius, offset.normalized, out _, offset.magnitude,
                ~(1 << 2), QueryTriggerInteraction.Ignore);
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
