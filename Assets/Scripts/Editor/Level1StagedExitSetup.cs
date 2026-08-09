#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.AI;
using NHNHackathon.Enemy;
using NHNHackathon.ExitSystem;
using NHNHackathon.Items;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class Level1StagedExitSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("NHN Hackathon/Setup/Level1 Staged Exit")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform zone6 = FindTransform("Zone6");
            ExitDoor door = zone6 != null ? zone6.GetComponentInChildren<ExitDoor>(true) : null;
            if (zone6 == null || door == null) throw new System.InvalidOperationException("Zone6/ExitDoor was not found.");

            GameObject host = door.gameObject;
            StagedExitUnlockController unlock = GetOrAdd<StagedExitUnlockController>(host);
            ExitUnlockStageEffects effects = GetOrAdd<ExitUnlockStageEffects>(host);

            SerializedObject doorSo = new SerializedObject(door);
            doorSo.FindProperty("directInteractionEnabled").boolValue = false;
            doorSo.ApplyModifiedPropertiesWithoutUndo();

            EnemyController watcher = zone6.GetComponentsInChildren<EnemyController>(true).FirstOrDefault();
            watcher ??= Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(value => Vector3.SqrMagnitude(value.transform.position - door.transform.position)).FirstOrDefault();

            EnemyPatrolRoute alternateRoute = null;
            if (watcher != null)
            {
                alternateRoute = zone6.GetComponentsInChildren<EnemyPatrolRoute>(true)
                    .FirstOrDefault(route => route != watcher.PatrolRoute);
                alternateRoute ??= Object.FindObjectsByType<EnemyPatrolRoute>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(route => route != watcher.PatrolRoute)
                    .OrderBy(route => Vector3.SqrMagnitude(route.transform.position - watcher.transform.position)).FirstOrDefault();
            }

            Transform lightsRoot = zone6.GetComponentsInChildren<Transform>(true)
                .Where(value => value.name == "Lights")
                .OrderByDescending(value => value.childCount).FirstOrDefault();
            if (lightsRoot == null || lightsRoot.childCount == 0)
            {
                lightsRoot = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(value => value.name == "Lights" && value.childCount > 0)
                    .OrderBy(value => Vector3.SqrMagnitude(value.position - door.transform.position))
                    .FirstOrDefault();
            }
            Light[] lights = lightsRoot != null ? lightsRoot.GetComponentsInChildren<Light>(true) : new Light[0];
            GameObject[] lightObjects = lightsRoot != null
                ? Enumerable.Range(0, lightsRoot.childCount)
                    .Select(index => lightsRoot.GetChild(index).gameObject).ToArray()
                : new GameObject[0];
            Transform crowdTarget = CreateCrowdTarget(zone6, door.transform);

            WatcherEventReactionController reaction = null;
            if (watcher != null)
            {
                reaction = GetOrAdd<WatcherEventReactionController>(watcher.gameObject);
                SerializedObject reactionSo = new SerializedObject(reaction);
                reactionSo.FindProperty("enemyController").objectReferenceValue = watcher;
                reactionSo.FindProperty("agent").objectReferenceValue = watcher.GetComponent<NavMeshAgent>();
                reactionSo.FindProperty("unlockReactionTarget").objectReferenceValue = door.transform;
                reactionSo.FindProperty("crowdTarget").objectReferenceValue = crowdTarget;
                reactionSo.ApplyModifiedPropertiesWithoutUndo();
            }

            ExitUnlockProgressUI progressUI = BuildProgressUI();
            ConfigureUnlock(unlock, progressUI);

            SerializedObject effectsSo = new SerializedObject(effects);
            effectsSo.FindProperty("exitDoor").objectReferenceValue = door;
            effectsSo.FindProperty("watcher").objectReferenceValue = watcher;
            effectsSo.FindProperty("watcherReaction").objectReferenceValue = reaction;
            effectsSo.FindProperty("secondStagePatrolRoute").objectReferenceValue = alternateRoute;
            SerializedProperty lightArray = effectsSo.FindProperty("corridorLights");
            lightArray.arraySize = lights.Length;
            for (int i = 0; i < lights.Length; i++) lightArray.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
            SerializedProperty lightObjectsArray = effectsSo.FindProperty("corridorLightObjects");
            lightObjectsArray.arraySize = lightObjects.Length;
            for (int i = 0; i < lightObjects.Length; i++) lightObjectsArray.GetArrayElementAtIndex(i).objectReferenceValue = lightObjects[i];
            effectsSo.ApplyModifiedPropertiesWithoutUndo();

            ClearEvent(unlock.Stages[0].OnCompleted);
            ClearEvent(unlock.Stages[1].OnCompleted);
            ClearEvent(unlock.Stages[2].OnCompleted);
            UnityEventTools.AddPersistentListener(unlock.Stages[0].OnCompleted, effects.PlayFirstUnlock);
            UnityEventTools.AddPersistentListener(unlock.Stages[1].OnCompleted, effects.PlaySecondUnlock);
            UnityEventTools.AddPersistentListener(unlock.Stages[2].OnCompleted, effects.PlayFinalUnlock);

            EditorUtility.SetDirty(unlock);
            EditorUtility.SetDirty(effects);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Level1 staged exit configured. Watcher={watcher?.name}, Route={alternateRoute?.name}, Lights={lights.Length}, LightObjects={lightObjects.Length}");
        }

        private static void ConfigureUnlock(StagedExitUnlockController unlock, ExitUnlockProgressUI ui)
        {
            SerializedObject so = new SerializedObject(unlock);
            so.FindProperty("progressUI").objectReferenceValue = ui;
            SerializedProperty stages = so.FindProperty("stages");
            stages.arraySize = 3;
            string[] labels = { "첫 번째 잠금장치 해제 중", "두 번째 잠금장치 해제 중", "마지막 잠금장치 해제 중" };
            float[] durations = { 2f, 3f, 4f };
            for (int i = 0; i < 3; i++)
            {
                SerializedProperty stage = stages.GetArrayElementAtIndex(i);
                stage.FindPropertyRelative("displayName").stringValue = labels[i];
                stage.FindPropertyRelative("requiredKey").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/Data/Items/Key/Key_0{i + 1}.asset");
                stage.FindPropertyRelative("unlockDuration").floatValue = durations[i];
                stage.FindPropertyRelative("consumeKeyOnComplete").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ExitUnlockProgressUI BuildProgressUI()
        {
            Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(value => value.transform.root.name == "UI")
                ?? Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            Transform old = canvas.transform.Find("ExitUnlockUI");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject root = CreateUIObject("ExitUnlockUI", canvas.transform);
            Stretch(root.GetComponent<RectTransform>());
            ExitUnlockProgressUI ui = root.AddComponent<ExitUnlockProgressUI>();
            GameObject panel = CreateUIObject("ProgressPanel", root.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.82f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.15f);
            panelRect.sizeDelta = new Vector2(520f, 120f);

            Text title = CreateText("StageText", panel.transform, 24, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 54f), new Vector2(480f, 34f));
            Image background = CreateUIObject("ProgressBarBackground", panel.transform).AddComponent<Image>();
            background.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            SetRect(background.rectTransform, new Vector2(0f, 8f), new Vector2(440f, 24f));
            Image fill = CreateUIObject("ProgressBarFill", background.transform).AddComponent<Image>();
            fill.color = new Color(0.75f, 0.1f, 0.08f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Stretch(fill.rectTransform);
            Text time = CreateText("TimeText", panel.transform, 18, TextAnchor.MiddleCenter);
            SetRect(time.rectTransform, new Vector2(0f, -35f), new Vector2(300f, 28f));

            SerializedObject uiSo = new SerializedObject(ui);
            uiSo.FindProperty("root").objectReferenceValue = root;
            uiSo.FindProperty("stageText").objectReferenceValue = title;
            uiSo.FindProperty("progressFill").objectReferenceValue = fill;
            uiSo.FindProperty("timeText").objectReferenceValue = time;
            uiSo.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return ui;
        }

        private static Transform CreateCrowdTarget(Transform zone6, Transform fallback)
        {
            Transform target = zone6.Find("CrowdLookTarget");
            if (target == null)
            {
                target = new GameObject("CrowdLookTarget").transform;
                target.SetParent(zone6, true);
            }
            DanceSequenceController[] dancers = zone6.GetComponentsInChildren<DanceSequenceController>(true);
            target.position = dancers.Length > 0
                ? dancers.Aggregate(Vector3.zero, (sum, dancer) => sum + dancer.transform.position) / dancers.Length
                : fallback.position + fallback.forward * 5f;
            return target;
        }

        private static void ClearEvent(UnityEvent value)
        {
            while (value.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(value, 0);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component => target.GetComponent<T>() ?? target.AddComponent<T>();
        private static Transform FindTransform(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(value => value.name == name);
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }
        private static Text CreateText(string name, Transform parent, int size, TextAnchor alignment)
        {
            Text value = CreateUIObject(name, parent).AddComponent<Text>();
            value.font = ProjectFontProvider.LoadRegular();
            value.fontSize = size;
            value.alignment = alignment;
            value.color = Color.white;
            return value;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
#endif
