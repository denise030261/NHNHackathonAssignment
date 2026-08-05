#if UNITY_EDITOR
using NHNHackathon.Enemy;
using UnityEditor;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class WatcherCaptureTriggerSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Characters/Watcher.prefab";

        [MenuItem("Tools/NHN Hackathon/Enemy/Build Watcher Capture Trigger")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Rigidbody body = root.GetComponent<Rigidbody>();
                if (body == null)
                {
                    body = root.AddComponent<Rigidbody>();
                }
                body.isKinematic = true;
                body.useGravity = false;

                Transform existing = root.transform.Find("CaptureTrigger");
                GameObject triggerObject;
                if (existing == null)
                {
                    triggerObject = new GameObject("CaptureTrigger");
                    triggerObject.transform.SetParent(root.transform, false);
                }
                else
                {
                    triggerObject = existing.gameObject;
                }

                triggerObject.layer = root.layer;
                triggerObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                triggerObject.transform.localRotation = Quaternion.identity;
                triggerObject.transform.localScale = Vector3.one;
                SphereCollider sphere = triggerObject.GetComponent<SphereCollider>();
                if (sphere == null)
                {
                    sphere = triggerObject.AddComponent<SphereCollider>();
                }
                sphere.isTrigger = true;
                sphere.radius = 0.75f;
                EnemyCaptureTrigger trigger = triggerObject.GetComponent<EnemyCaptureTrigger>();
                if (trigger == null)
                {
                    trigger = triggerObject.AddComponent<EnemyCaptureTrigger>();
                }
                SerializedObject values = new(trigger);
                values.FindProperty("enemyController").objectReferenceValue =
                    root.GetComponent<EnemyController>();
                values.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("WATCHER_CAPTURE_TRIGGER_COMPLETE");
        }
    }
}
#endif
