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
                    Transform rightHand = model.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(value => value.name == "hand.R");
                    socket = new GameObject("ThirdPersonFlashlightSocket").transform;
                    socket.SetParent(rightHand != null ? rightHand : model, false);
                }

                socket.localPosition = Vector3.zero;
                socket.localRotation = Quaternion.identity;
                socket.localScale = Vector3.one;

                Transform heldMesh = socket.Find("HeldFlashlightMesh");
                if (heldMesh == null)
                {
                    GameObject modelAsset =
                        AssetDatabase.LoadAssetAtPath<GameObject>(FlashlightModelPath)
                        ?? throw new System.InvalidOperationException(
                            "Flashlight model was not found.");
                    heldMesh = ((GameObject)PrefabUtility.InstantiatePrefab(
                        modelAsset, socket)).transform;
                    heldMesh.name = "HeldFlashlightMesh";
                }
                heldMesh.SetLocalPositionAndRotation(
                    Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
                heldMesh.localScale = Vector3.one * 1.25f;

                SerializedObject values = new SerializedObject(flashlight);
                values.FindProperty("cameraController").objectReferenceValue = cameraController;
                values.FindProperty("firstPersonParent").objectReferenceValue = camera.transform;
                values.FindProperty("firstPersonLocalPosition").vector3Value = new Vector3(0f, 0f, 0.15f);
                values.FindProperty("firstPersonLocalEulerAngles").vector3Value = Vector3.zero;
                values.FindProperty("thirdPersonParent").objectReferenceValue = root.transform;
                values.FindProperty("thirdPersonLocalPosition").vector3Value =
                    new Vector3(0f, 1.35f, 0.4f);
                values.FindProperty("thirdPersonLocalEulerAngles").vector3Value = Vector3.zero;
                values.FindProperty("attachmentTransitionDuration").floatValue = 0.45f;
                values.FindProperty("heldFlashlightMesh").objectReferenceValue =
                    heldMesh.gameObject;
                values.FindProperty("heldFlashlightMeshScale").floatValue = 1.25f;
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
