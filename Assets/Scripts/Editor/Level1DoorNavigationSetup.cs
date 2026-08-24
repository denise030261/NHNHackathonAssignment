#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.ExitSystem;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1DoorNavigationSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string LinkName = "DoorNavMeshLink";

        [MenuItem("Tools/NHN Hackathon/Level1/Configure Dynamic Door Navigation")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ExitDoor[] doors = Object.FindObjectsByType<ExitDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            NavMeshAgent sampleAgent = Object.FindFirstObjectByType<NavMeshAgent>(
                FindObjectsInactive.Include);

            foreach (ExitDoor door in doors)
            {
                ConfigureDoor(door, sampleAgent);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"LEVEL1_DYNAMIC_DOOR_NAVIGATION_COMPLETE: {doors.Length} doors configured.");
        }

        private static void ConfigureDoor(ExitDoor door, NavMeshAgent sampleAgent)
        {
            DoorNavigationController controller =
                door.GetComponent<DoorNavigationController>()
                ?? Undo.AddComponent<DoorNavigationController>(door.gameObject);

            Transform linkRoot = door.transform.Cast<Transform>()
                .FirstOrDefault(child => child.name == LinkName);
            if (linkRoot == null)
            {
                GameObject linkObject = new(LinkName);
                Undo.RegisterCreatedObjectUndo(linkObject, "Create Door NavMeshLink");
                linkRoot = linkObject.transform;
                linkRoot.SetParent(door.transform, false);
            }

            NavMeshLink link = linkRoot.GetComponent<NavMeshLink>()
                ?? Undo.AddComponent<NavMeshLink>(linkRoot.gameObject);
            ConfigureLinkTransform(door, linkRoot, link);
            link.agentTypeID = sampleAgent != null ? sampleAgent.agentTypeID : 0;
            link.bidirectional = true;
            link.autoUpdate = false;
            link.costModifier = -1f;
            link.activated = false;

            SerializedObject controllerValues = new(controller);
            controllerValues.FindProperty("door").objectReferenceValue = door;
            controllerValues.FindProperty("navigationLink").objectReferenceValue = link;
            controllerValues.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject doorValues = new(door);
            doorValues.FindProperty("navigationController").objectReferenceValue = controller;
            doorValues.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(link);
        }

        private static void ConfigureLinkTransform(
            ExitDoor door, Transform linkRoot, NavMeshLink link)
        {
            Collider blocker = door.GetComponentsInChildren<Collider>(true)
                .FirstOrDefault(value => !value.isTrigger);
            Vector3 center = door.transform.position;
            float width = 1.2f;
            float depth = 0.5f;

            if (blocker != null)
            {
                Bounds bounds = blocker.bounds;
                center = bounds.center;
                center.y = bounds.min.y + 0.05f;
                width = ProjectSize(bounds.size, door.transform.right);
                depth = ProjectSize(bounds.size, door.transform.forward);
            }

            linkRoot.SetPositionAndRotation(center, door.transform.rotation);
            linkRoot.localScale = Vector3.one;
            float halfLength = Mathf.Max(1f, depth * 0.5f + 0.65f);
            link.startPoint = Vector3.back * halfLength;
            link.endPoint = Vector3.forward * halfLength;
            link.width = Mathf.Max(0.5f, width - 0.25f);
        }

        private static float ProjectSize(Vector3 axisAlignedSize, Vector3 direction)
        {
            direction = new Vector3(
                Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            return Vector3.Dot(axisAlignedSize, direction);
        }
    }
}
#endif
