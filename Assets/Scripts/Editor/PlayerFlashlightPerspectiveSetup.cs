#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Characters;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class PlayerFlashlightPerspectiveSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Characters/Player.prefab";
        private const string FlashlightModelPath =
            "Assets/Art/Items/flashlight/Flashlight.fbx";

        [MenuItem("NHN Hackathon/Setup/Player Flashlight Perspective Attachment")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                PlayerCameraController cameraController = root.GetComponent<PlayerCameraController>();
                Camera camera = root.GetComponentInChildren<Camera>(true);
                PlayerFlashlightController flashlight =
                    root.GetComponentInChildren<PlayerFlashlightController>(true);
                Transform model = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => value.name == "CharacterModel")
                    ?? throw new System.InvalidOperationException("CharacterModel was not found.");

                Transform socket = model.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => value.name == "ThirdPersonFlashlightSocket");
                if (socket == null)
                {
                    socket = new GameObject("ThirdPersonFlashlightSocket").transform;
                    socket.SetParent(model, true);
                }

                // Default shoulder/hand-side position. Designers can adjust this socket directly.
                socket.position = root.transform.TransformPoint(new Vector3(0.32f, 0.95f, 0.28f));
                socket.rotation = root.transform.rotation;
                socket.localScale = Vector3.one;

                Transform flashlightMesh = flashlight.transform.Find("HeldFlashlightMesh");
                if (flashlightMesh == null)
                {
                    GameObject flashlightModel =
                        AssetDatabase.LoadAssetAtPath<GameObject>(FlashlightModelPath)
                        ?? throw new System.InvalidOperationException(
                            "Flashlight model was not found.");
                    flashlightMesh = ((GameObject)PrefabUtility.InstantiatePrefab(
                        flashlightModel, flashlight.transform)).transform;
                    flashlightMesh.name = "HeldFlashlightMesh";
                }
                flashlightMesh.SetLocalPositionAndRotation(
                    Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
                flashlightMesh.localScale = Vector3.one * 1.25f;

                SerializedObject values = new SerializedObject(flashlight);
                values.FindProperty("cameraController").objectReferenceValue = cameraController;
                values.FindProperty("firstPersonParent").objectReferenceValue = camera.transform;
                values.FindProperty("firstPersonLocalPosition").vector3Value = new Vector3(0f, 0f, 0.15f);
                values.FindProperty("firstPersonLocalEulerAngles").vector3Value = Vector3.zero;
                values.FindProperty("thirdPersonParent").objectReferenceValue = socket;
                values.FindProperty("thirdPersonLocalPosition").vector3Value = Vector3.zero;
                values.FindProperty("thirdPersonLocalEulerAngles").vector3Value = Vector3.zero;
                values.FindProperty("attachmentTransitionDuration").floatValue = 0.45f;
                values.FindProperty("flashlightMeshRoot").objectReferenceValue =
                    flashlightMesh.gameObject;
                values.FindProperty("showMeshInFirstPerson").boolValue = false;
                values.FindProperty("flashlightMeshScale").floatValue = 1.25f;
                values.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("PLAYER_FLASHLIGHT_PERSPECTIVE_SETUP_COMPLETE");
        }
    }
}
#endif
