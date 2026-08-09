#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.Enemy;
using NHNHackathon.Game;
using NHNHackathon.Inspection;
using NHNHackathon.Input;
using NHNHackathon.Interaction;
using NHNHackathon.Inventory;
using NHNHackathon.LightSystem;
using NHNHackathon.Pause;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class Level1EnemyCaptureSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Build Enemy Capture Game Over")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject ui = Find("UI") ?? throw new System.InvalidOperationException("UI root missing.");
            Canvas canvas = ui.GetComponentInChildren<Canvas>(true)
                ?? throw new System.InvalidOperationException("Canvas missing under UI.");
            GameOverController controller = Object.FindAnyObjectByType<GameOverController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                GameObject system = new("GameOverSystem");
                system.transform.SetParent(ui.transform, false);
                controller = system.AddComponent<GameOverController>();
            }

            Transform old = canvas.transform.Find("GameOverUI");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Font font = ProjectFontProvider.LoadRegular();
            GameObject root = Image("GameOverUI", canvas.transform, Vector2.zero, Vector2.one, Color.clear);
            GameObject fade = Image("FadeOverlay", root.transform, Vector2.zero, Vector2.one, Color.clear);
            fade.GetComponent<Image>().raycastTarget = false;
            GameObject content = Image("GameOverContent", root.transform,
                new Vector2(0.34f, 0.25f), new Vector2(0.66f, 0.75f), new Color(0.035f, 0.025f, 0.03f, 0.97f));
            Text title = Text("GameOverText", content.transform, "GAME OVER",
                new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.9f), 58, font, new Color(0.72f, 0.08f, 0.08f));
            Button restart = Button("RestartButton", content.transform, "RESTART",
                new Vector2(0.14f, 0.40f), new Vector2(0.86f, 0.56f), font);
            Button menu = Button("MainMenuButton", content.transform, "MAIN MENU",
                new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.32f), font);

            EnemyCaptureDirector director = controller.GetComponent<EnemyCaptureDirector>()
                ?? controller.gameObject.AddComponent<EnemyCaptureDirector>();
            SerializedObject directorValues = new(director);
            directorValues.FindProperty("playerCamera").objectReferenceValue = Camera.main;
            directorValues.FindProperty("fadeOverlay").objectReferenceValue = fade.GetComponent<Image>();
            directorValues.ApplyModifiedPropertiesWithoutUndo();

            Behaviour[] controls =
            {
                Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerCameraController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerDanceInput>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerCursorController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerFlashlightController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerInteractor>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<InventoryController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<ItemInspectionController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include)
            };
            SerializedObject values = new(controller);
            values.FindProperty("captureDirector").objectReferenceValue = director;
            values.FindProperty("gameOverUI").objectReferenceValue = root;
            values.FindProperty("gameOverContent").objectReferenceValue = content;
            values.FindProperty("gameOverText").objectReferenceValue = title;
            values.FindProperty("restartButton").objectReferenceValue = restart;
            values.FindProperty("mainMenuButton").objectReferenceValue = menu;
            values.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            SerializedProperty array = values.FindProperty("playerControls");
            array.arraySize = controls.Length;
            for (int i = 0; i < controls.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = controls[i];
            values.ApplyModifiedPropertiesWithoutUndo();
            Bind(restart, controller.RestartGame);
            Bind(menu, controller.ReturnToMainMenu);

            foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (enemy.GetComponent<WatcherCapturePresenter>() == null)
                    enemy.gameObject.AddComponent<WatcherCapturePresenter>();
                SerializedObject enemyValues = new(enemy);
                enemyValues.FindProperty("gameOverController").objectReferenceValue = controller;
                enemyValues.ApplyModifiedPropertiesWithoutUndo();
            }

            content.SetActive(false);
            root.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_ENEMY_CAPTURE_COMPLETE");
        }

        private static GameObject Find(string name) => Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(value => value.scene.IsValid() && value.name == name);

        private static GameObject Image(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            Rect(item.GetComponent<RectTransform>(), min, max);
            item.GetComponent<Image>().color = color;
            return item;
        }

        private static Text Text(string name, Transform parent, string label, Vector2 min, Vector2 max, int size, Font font, Color color)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            Rect(item.GetComponent<RectTransform>(), min, max);
            Text text = item.GetComponent<Text>();
            text.text = label; text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }

        private static Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max, Font font)
        {
            GameObject item = Image(name, parent, min, max, new Color(0.15f, 0.12f, 0.13f, 1f));
            Button button = item.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.42f, 0.16f, 0.16f, 1f);
            colors.pressedColor = new Color(0.08f, 0.05f, 0.06f, 1f);
            button.colors = colors;
            Text("Text", item.transform, label, Vector2.zero, Vector2.one, 25, font, Color.white);
            return button;
        }

        private static void Rect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }
    }
}
#endif
