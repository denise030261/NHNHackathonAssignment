#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.ExitSystem;
using NHNHackathon.Game;
using NHNHackathon.Inspection;
using NHNHackathon.Interaction;
using NHNHackathon.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class DoYoungPracticeUIConsolidationSetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";

        [MenuItem("Tools/NHN Hackathon/UI/Consolidate DoYoung Practice UI")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform uiRoot = GetOrCreateRoot("UI", null).transform;
            Canvas canvas = GetOrCreateSharedCanvas(uiRoot);
            Transform canvasRoot = canvas.transform;

            GameObject hud = GetOrCreatePanel("HUD", canvasRoot);
            ConfigureHud(hud.transform);

            GameObject inventory = MovePanel("InventoryCanvas", "InventoryUI", canvasRoot);
            GameObject inspection = MovePanel("ItemInspectionCanvas", "ItemInspectionUI", canvasRoot);
            GameObject paperReader = FindSceneObject("PaperReader");
            if (paperReader != null)
            {
                paperReader.name = "PaperReadingUI";
            }
            GameObject gameOver = MovePanel("GameOverUI", "GameOverUI", canvasRoot);
            ConfigureGameSuccess(canvasRoot);

            SetSibling(canvasRoot, hud, 0);
            SetSibling(canvasRoot, inventory, 1);
            SetSibling(canvasRoot, inspection, 2);
            SetSibling(canvasRoot, gameOver, 3);
            GameObject success = FindSceneObject("GameSuccessUI");
            SetSibling(canvasRoot, success, 4);

            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                eventSystem.transform.SetParent(uiRoot, true);
            }

            EnsureInspectionPausesWorld();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Shared UI/Canvas hierarchy created and all game UI panels consolidated.");
        }

        private static Canvas GetOrCreateSharedCanvas(Transform uiRoot)
        {
            Transform existing = uiRoot.Find("Canvas");
            GameObject root = existing != null ? existing.gameObject :
                new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(uiRoot, false);

            Canvas canvas = GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            GetOrAdd<GraphicRaycaster>(root);
            return canvas;
        }

        private static void ConfigureHud(Transform hud)
        {
            Font font = ProjectFontProvider.LoadRegular();
            GameObject prompt = GetOrCreateText(
                "InteractionPrompt", hud, new Vector2(0f, 0.70f),
                new Vector2(1f, 0.77f), 26, Color.white, TextAnchor.MiddleCenter, font);
            GameObject keyCounter = GetOrCreateText(
                "KeyCounter", hud, new Vector2(0.016f, 0.925f),
                new Vector2(0.25f, 0.98f), 28, Color.white, TextAnchor.UpperLeft, font);
            GameObject notification = GetOrCreateText(
                "KeyNotification", hud, new Vector2(0f, 0.65f),
                new Vector2(1f, 0.72f), 32, new Color(1f, 0.75f, 0.2f),
                TextAnchor.MiddleCenter, font);

            PlayerInteractor interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            if (interactor != null)
            {
                SerializedObject settings = new SerializedObject(interactor);
                settings.FindProperty("promptRoot").objectReferenceValue = prompt;
                settings.FindProperty("promptText").objectReferenceValue = prompt.GetComponent<Text>();
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            prompt.SetActive(false);
            keyCounter.SetActive(false);
            notification.SetActive(false);
        }

        private static void ConfigureGameSuccess(Transform canvasRoot)
        {
            GameSuccessController controller = Object.FindAnyObjectByType<GameSuccessController>();
            if (controller == null)
            {
                return;
            }

            GameObject root = GetOrCreatePanel("GameSuccessUI", canvasRoot);
            ClearChildren(root.transform);
            GameObject overlay = CreateImage("Overlay", root.transform, Color.black);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            GameObject textObject = GetOrCreateText(
                "SuccessText", overlay.transform, Vector2.zero, Vector2.one,
                72, new Color(0.7f, 1f, 0.75f), TextAnchor.MiddleCenter,
                ProjectFontProvider.LoadRegular());
            Text label = textObject.GetComponent<Text>();
            label.text = "ESCAPED";
            label.fontStyle = FontStyle.Bold;

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("gameSuccessUI").objectReferenceValue = root;
            settings.FindProperty("successText").objectReferenceValue = label;
            settings.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
        }

        private static GameObject MovePanel(string oldName, string newName, Transform canvasRoot)
        {
            GameObject panel = FindSceneObject(oldName) ?? FindSceneObject(newName);
            if (panel == null)
            {
                return null;
            }

            RemoveIfPresent<Canvas>(panel);
            RemoveIfPresent<CanvasScaler>(panel);
            RemoveIfPresent<GraphicRaycaster>(panel);
            panel.name = newName;
            panel.transform.SetParent(canvasRoot, false);
            Stretch(GetOrAdd<RectTransform>(panel));
            return panel;
        }

        private static void EnsureInspectionPausesWorld()
        {
            foreach (InspectionControlLock controlLock in
                     Resources.FindObjectsOfTypeAll<InspectionControlLock>()
                         .Where(value => value.gameObject.scene.IsValid()))
            {
                SerializedObject settings = new SerializedObject(controlLock);
                settings.FindProperty("pauseWorldDuringInspection").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject GetOrCreatePanel(string name, Transform parent)
        {
            GameObject panel = FindSceneObject(name);
            if (panel == null)
            {
                panel = new GameObject(name, typeof(RectTransform));
            }
            panel.transform.SetParent(parent, false);
            Stretch(GetOrAdd<RectTransform>(panel));
            return panel;
        }

        private static GameObject GetOrCreateText(
            string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            int fontSize, Color color, TextAnchor alignment, Font font)
        {
            Transform existing = parent.Find(name);
            GameObject root = existing != null ? existing.gameObject :
                new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            RectTransform rect = GetOrAdd<RectTransform>(root);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = GetOrAdd<Text>(root);
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return root;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = color;
            return root;
        }

        private static GameObject GetOrCreateRoot(string name, Transform parent)
        {
            GameObject root = FindSceneObject(name) ?? new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }
            return root;
        }

        private static GameObject FindSceneObject(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(value => value.name == name && value.scene.IsValid());
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(parent.GetChild(index).gameObject);
            }
        }

        private static void SetSibling(Transform expectedParent, GameObject target, int index)
        {
            if (target != null && target.transform.parent == expectedParent)
            {
                target.transform.SetSiblingIndex(index);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            return root.GetComponent<T>() ?? root.AddComponent<T>();
        }

        private static void RemoveIfPresent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}
#endif
