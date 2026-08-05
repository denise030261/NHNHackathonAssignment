#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.ExitSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    public static class Level1Zone12ExitDoorSlamSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Build Zone1_2 Exit Door Slam")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject zone = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Zone1_2")
                ?? throw new System.InvalidOperationException("Zone1_2 was not found.");
            ExitDoor door = zone.GetComponentsInChildren<ExitDoor>(true)
                .FirstOrDefault(value => value.name.Trim() == "ExitDoor")
                ?? throw new System.InvalidOperationException("Zone1_2/ExitDoor was not found.");

            Transform existing = door.transform.Find("AutoSlamPassageTrigger");
            GameObject triggerObject;
            if (existing == null)
            {
                triggerObject = new GameObject("AutoSlamPassageTrigger");
                triggerObject.transform.SetParent(door.transform, false);
            }
            else
            {
                triggerObject = existing.gameObject;
            }

            triggerObject.layer = door.gameObject.layer;
            triggerObject.transform.localPosition = new Vector3(2f, 1.1f, 0f);
            triggerObject.transform.localRotation = Quaternion.identity;
            triggerObject.transform.localScale = Vector3.one;
            BoxCollider trigger = triggerObject.GetComponent<BoxCollider>();
            if (trigger == null)
            {
                trigger = triggerObject.AddComponent<BoxCollider>();
            }
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.size = new Vector3(3.2f, 2.2f, 2.4f);

            DoorPassageAutoSlam slam = triggerObject.GetComponent<DoorPassageAutoSlam>();
            if (slam == null)
            {
                slam = triggerObject.AddComponent<DoorPassageAutoSlam>();
            }
            SerializedObject values = new(slam);
            values.FindProperty("door").objectReferenceValue = door;
            values.FindProperty("crossingAxis").objectReferenceValue = door.transform;
            values.FindProperty("slamDuration").floatValue = 0.18f;
            values.FindProperty("oneShot").boolValue = true;
            values.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_ZONE12_EXIT_DOOR_SLAM_COMPLETE");
        }
    }
}
#endif
