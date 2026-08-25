#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.ExitSystem;
using NHNHackathon.Game;
using NHNHackathon.Input;
using NHNHackathon.Inspection;
using NHNHackathon.Interaction;
using NHNHackathon.Inventory;
using NHNHackathon.LightSystem;
using NHNHackathon.MainMenu;
using NHNHackathon.Pause;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class Level1PauseMenuSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/Level1/Build Pause Menu")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject uiRoot = FindSceneObject("UI")
                ?? throw new System.InvalidOperationException("Level1 requires UI root.");
            Canvas canvas = uiRoot.GetComponentInChildren<Canvas>(true)
                ?? throw new System.InvalidOperationException("Level1 UI requires Canvas.");

            DeleteSceneObject("PauseMenuSystem");
            Transform oldUi = canvas.transform.Find("PauseMenuUI");
            if (oldUi != null)
            {
                Object.DestroyImmediate(oldUi.gameObject);
            }

            Font font = ProjectFontProvider.LoadRegular();
            GameObject pauseRoot = CreatePanel(
                "PauseMenuUI", canvas.transform, new Color(0f, 0f, 0f, 0.72f));
            GameObject pausePanel = BuildPausePanel(
                pauseRoot.transform, font,
                out Button resumeButton, out Button settingsButton, out Button quitButton);
            GameObject settingsPanel = BuildSettingsPanel(
                pauseRoot.transform, font,
                out Slider bgmSlider, out Slider sfxSlider,
                out Text bgmValue, out Text sfxValue, out Button closeButton);

            GameObject system = new("PauseMenuSystem");
            system.transform.SetParent(uiRoot.transform, false);
            PauseMenuController pauseController = system.AddComponent<PauseMenuController>();
            AudioSettingsController audioController = system.AddComponent<AudioSettingsController>();
            ConfigurePauseController(
                pauseController, pauseRoot, pausePanel, settingsPanel);
            ConfigureAudioController(
                audioController, bgmSlider, sfxSlider, bgmValue, sfxValue);
            BindButton(resumeButton, pauseController.ResumeGame);
            BindButton(settingsButton, pauseController.OpenSettings);
            BindButton(quitButton, pauseController.ReturnToMainMenu);
            BindButton(closeButton, pauseController.CloseSettings);
            BindSliders(audioController, bgmSlider, sfxSlider);

            PlayerCursorController cursor =
                Object.FindAnyObjectByType<PlayerCursorController>(FindObjectsInactive.Include);
            if (cursor != null)
            {
                SerializedObject cursorSettings = new(cursor);
                cursorSettings.FindProperty("handleEscapeInput").boolValue = false;
                cursorSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            pauseRoot.SetActive(false);
            settingsPanel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_PAUSE_MENU_COMPLETE: resume, audio settings, and main menu return connected.");
        }

        private static GameObject BuildPausePanel(
            Transform parent, Font font,
            out Button resume, out Button settings, out Button quit)
        {
            GameObject panel = CreateImage(
                "PausePanel", parent,
                new Vector2(0.37f, 0.24f), new Vector2(0.63f, 0.76f),
                new Color(0.08f, 0.08f, 0.1f, 0.98f));
            CreateText(
                "TitleText", panel.transform, "PAUSED",
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                42, font);
            resume = CreateButton(
                "ResumeButton", panel.transform, "게임 재개",
                new Vector2(0.12f, 0.57f), new Vector2(0.88f, 0.72f), font);
            settings = CreateButton(
                "SettingsButton", panel.transform, "환경설정",
                new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.51f), font);
            quit = CreateButton(
                "ReturnToMainMenuButton", panel.transform, "게임 끝내기",
                new Vector2(0.12f, 0.15f), new Vector2(0.88f, 0.30f), font);
            return panel;
        }

        private static GameObject BuildSettingsPanel(
            Transform parent, Font font,
            out Slider bgmSlider, out Slider sfxSlider,
            out Text bgmValue, out Text sfxValue, out Button close)
        {
            GameObject panel = CreateImage(
                "SettingsPanel", parent,
                new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.80f),
                new Color(0.08f, 0.08f, 0.1f, 0.99f));
            CreateText(
                "TitleText", panel.transform, "음향 설정",
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                42, font);
            CreateText(
                "BGMLabel", panel.transform, "BGM",
                new Vector2(0.10f, 0.57f), new Vector2(0.28f, 0.67f), 28, font);
            bgmSlider = CreateSlider(
                "BGMSlider", panel.transform,
                new Vector2(0.29f, 0.57f), new Vector2(0.78f, 0.67f));
            bgmValue = CreateText(
                "BGMValueText", panel.transform, "100%",
                new Vector2(0.80f, 0.57f), new Vector2(0.92f, 0.67f), 24, font);
            CreateText(
                "SFXLabel", panel.transform, "SFX",
                new Vector2(0.10f, 0.40f), new Vector2(0.28f, 0.50f), 28, font);
            sfxSlider = CreateSlider(
                "SFXSlider", panel.transform,
                new Vector2(0.29f, 0.40f), new Vector2(0.78f, 0.50f));
            sfxValue = CreateText(
                "SFXValueText", panel.transform, "100%",
                new Vector2(0.80f, 0.40f), new Vector2(0.92f, 0.50f), 24, font);
            close = CreateButton(
                "CloseButton", panel.transform, "뒤로",
                new Vector2(0.35f, 0.13f), new Vector2(0.65f, 0.25f), font);
            return panel;
        }

        private static void ConfigurePauseController(
            PauseMenuController controller, GameObject root,
            GameObject pausePanel, GameObject settingsPanel)
        {
            InventoryController inventory =
                Object.FindAnyObjectByType<InventoryController>(FindObjectsInactive.Include);
            ItemInspectionController inspection =
                Object.FindAnyObjectByType<ItemInspectionController>(FindObjectsInactive.Include);
            GameOverController gameOver =
                Object.FindAnyObjectByType<GameOverController>(FindObjectsInactive.Include);
            GameSuccessController success =
                Object.FindAnyObjectByType<GameSuccessController>(FindObjectsInactive.Include);
            Behaviour[] controls =
            {
                Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerCameraController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerDanceInput>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerCursorController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerFlashlightController>(FindObjectsInactive.Include),
                Object.FindAnyObjectByType<PlayerInteractor>(FindObjectsInactive.Include),
                inventory,
                inspection
            };

            SerializedObject values = new(controller);
            values.FindProperty("pauseRoot").objectReferenceValue = root;
            values.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            values.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            values.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            values.FindProperty("inventoryController").objectReferenceValue = inventory;
            values.FindProperty("inspectionController").objectReferenceValue = inspection;
            values.FindProperty("gameOverController").objectReferenceValue = gameOver;
            values.FindProperty("gameSuccessController").objectReferenceValue = success;
            SerializedProperty controlled = values.FindProperty("controlledBehaviours");
            controlled.arraySize = controls.Length;
            for (int index = 0; index < controls.Length; index++)
            {
                controlled.GetArrayElementAtIndex(index).objectReferenceValue = controls[index];
            }
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAudioController(
            AudioSettingsController controller, Slider bgmSlider, Slider sfxSlider,
            Text bgmText, Text sfxText)
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            AudioSource bgm = sources.FirstOrDefault(
                source => source.name.ToLowerInvariant().Contains("bgm"));
            AudioSource[] sfx = sources.Where(source => source != bgm).ToArray();

            SerializedObject values = new(controller);
            values.FindProperty("bgmSource").objectReferenceValue = bgm;
            SerializedProperty sfxSources = values.FindProperty("sfxSources");
            sfxSources.arraySize = sfx.Length;
            for (int index = 0; index < sfx.Length; index++)
            {
                sfxSources.GetArrayElementAtIndex(index).objectReferenceValue = sfx[index];
            }
            values.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
            values.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            values.FindProperty("bgmValueText").objectReferenceValue = bgmText;
            values.FindProperty("sfxValueText").objectReferenceValue = sfxText;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static void BindSliders(
            AudioSettingsController controller, Slider bgm, Slider sfx)
        {
            bgm.onValueChanged = new Slider.SliderEvent();
            sfx.onValueChanged = new Slider.SliderEvent();
            UnityEventTools.AddPersistentListener(bgm.onValueChanged, controller.SetBgmVolume);
            UnityEventTools.AddPersistentListener(sfx.onValueChanged, controller.SetSfxVolume);
            EditorUtility.SetDirty(bgm);
            EditorUtility.SetDirty(sfx);
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            return CreateImage(name, parent, Vector2.zero, Vector2.one, color);
        }

        private static GameObject CreateImage(
            string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), min, max);
            root.GetComponent<Image>().color = color;
            return root;
        }

        private static Text CreateText(
            string name, Transform parent, string value,
            Vector2 min, Vector2 max, int size, Font font)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), min, max);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name, Transform parent, string label,
            Vector2 min, Vector2 max, Font font)
        {
            GameObject root = CreateImage(
                name, parent, min, max, new Color(0.18f, 0.18f, 0.22f, 1f));
            Button button = root.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.44f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.13f, 1f);
            button.colors = colors;
            CreateText("Text", root.transform, label, Vector2.zero, Vector2.one, 28, font);
            return button;
        }

        private static Slider CreateSlider(
            string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), min, max);
            GameObject background = CreateImage(
                "Background", root.transform,
                new Vector2(0f, 0.38f), new Vector2(1f, 0.62f),
                new Color(0.18f, 0.18f, 0.2f, 1f));
            GameObject fillArea = new("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            SetRect(fillArea.GetComponent<RectTransform>(),
                new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.62f));
            GameObject fill = CreateImage(
                "Fill", fillArea.transform, Vector2.zero, Vector2.one,
                new Color(0.72f, 0.72f, 0.82f, 1f));
            GameObject handleArea = new("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            SetRect(handleArea.GetComponent<RectTransform>(),
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
            GameObject handle = CreateImage(
                "Handle", handleArea.transform,
                new Vector2(0f, 0.15f), new Vector2(0.045f, 0.85f), Color.white);
            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            background.GetComponent<Image>().raycastTarget = false;
            return slider;
        }

        private static void SetRect(
            RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static GameObject FindSceneObject(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(value => value.scene.IsValid() && value.name == name);
        }

        private static void DeleteSceneObject(string name)
        {
            GameObject target = FindSceneObject(name);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
