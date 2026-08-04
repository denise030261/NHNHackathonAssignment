#if UNITY_EDITOR
using System.Linq;
using NHNHackathon.MainMenu;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class MainMenuSceneSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Level1ScenePath = "Assets/Scenes/Level1.unity";
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        [MenuItem("Tools/NHN Hackathon/Main Menu/Build Main Menu Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();

            GameObject audioRoot = new("Audio");
            AudioSource bgmSource = CreateAudioSource("BGM AudioSource", audioRoot.transform, true);
            AudioSource sfxSource = CreateAudioSource("Menu SFX AudioSource", audioRoot.transform, false);

            GameObject uiRoot = new("UI");
            Canvas canvas = CreateCanvas(uiRoot.transform);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject mainPanel = CreatePanel(
                "MainPanel", canvas.transform, new Color(0.035f, 0.035f, 0.045f, 1f));
            BuildMainPanel(mainPanel.transform, font,
                out Button startButton, out Button settingsButton, out Button quitButton,
                out GameObject webQuitMessage);

            GameObject settingsPanel = BuildSettingsPanel(
                canvas.transform, font,
                out Slider bgmSlider, out Slider sfxSlider,
                out Text bgmValue, out Text sfxValue, out Button closeButton);

            GameObject system = new("MainMenuSystem");
            MainMenuController menuController = system.AddComponent<MainMenuController>();
            AudioSettingsController audioController = system.AddComponent<AudioSettingsController>();

            ConfigureMenuController(
                menuController, mainPanel, settingsPanel, webQuitMessage,
                startButton, settingsButton, quitButton);
            ConfigureAudioController(
                audioController, bgmSource, sfxSource,
                bgmSlider, sfxSlider, bgmValue, sfxValue);
            BindButtons(
                menuController, startButton, settingsButton, quitButton, closeButton);
            BindSliders(audioController, bgmSlider, sfxSlider);

            settingsPanel.SetActive(false);
            webQuitMessage.SetActive(false);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("MAIN_MENU_SCENE_COMPLETE: uGUI menu, audio settings, and build scenes configured.");
        }

        private static void CreateCamera()
        {
            GameObject root = new("Main Camera", typeof(Camera), typeof(AudioListener));
            root.tag = "MainCamera";
            Camera camera = root.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
        }

        private static void CreateEventSystem()
        {
            new GameObject(
                "EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static AudioSource CreateAudioSource(
            string name, Transform parent, bool loop)
        {
            GameObject root = new(name, typeof(AudioSource));
            root.transform.SetParent(parent, false);
            AudioSource source = root.GetComponent<AudioSource>();
            source.playOnAwake = loop;
            source.loop = loop;
            return source;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject root = new(
                "Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void BuildMainPanel(
            Transform parent, Font font,
            out Button startButton, out Button settingsButton, out Button quitButton,
            out GameObject webQuitMessage)
        {
            CreateImage(
                "GameImagePlaceholder", parent,
                new Vector2(0.04f, 0.1f), new Vector2(0.64f, 0.9f),
                new Color(0.075f, 0.075f, 0.09f, 1f));
            Text description = CreateText(
                "DescriptionText", parent,
                "어두운 폐저택에서 인형들의 춤을 따라 하며 탈출하십시오.",
                new Vector2(0.08f, 0.35f), new Vector2(0.60f, 0.62f),
                34, Color.white, TextAnchor.MiddleCenter, font);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Overflow;

            CreateImage(
                "TitleBackground", parent,
                new Vector2(0.69f, 0.65f), new Vector2(0.94f, 0.91f),
                new Color(0.12f, 0.12f, 0.15f, 1f));
            CreateText(
                "TitleText", parent, "MARIONETTE",
                new Vector2(0.69f, 0.65f), new Vector2(0.94f, 0.91f),
                50, Color.white, TextAnchor.MiddleCenter, font);

            startButton = CreateButton(
                "StartButton", parent, "게임 시작",
                new Vector2(0.69f, 0.47f), new Vector2(0.94f, 0.57f), font);
            settingsButton = CreateButton(
                "SettingsButton", parent, "환경설정",
                new Vector2(0.69f, 0.33f), new Vector2(0.94f, 0.43f), font);
            quitButton = CreateButton(
                "QuitButton", parent, "게임 종료",
                new Vector2(0.69f, 0.19f), new Vector2(0.94f, 0.29f), font);

            webQuitMessage = CreateImage(
                "WebQuitMessage", parent,
                new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.12f),
                new Color(0f, 0f, 0f, 0.9f));
            CreateText(
                "MessageText", webQuitMessage.transform,
                "게임을 종료하려면 브라우저 탭을 닫아주세요.",
                Vector2.zero, Vector2.one, 26, Color.white,
                TextAnchor.MiddleCenter, font);
        }

        private static GameObject BuildSettingsPanel(
            Transform parent, Font font,
            out Slider bgmSlider, out Slider sfxSlider,
            out Text bgmValue, out Text sfxValue, out Button closeButton)
        {
            GameObject panel = CreatePanel(
                "SettingsPanel", parent, new Color(0f, 0f, 0f, 0.82f));
            GameObject window = CreateImage(
                "Window", panel.transform,
                new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.84f),
                new Color(0.09f, 0.09f, 0.11f, 1f));
            CreateText(
                "TitleText", window.transform, "음향 설정",
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                44, Color.white, TextAnchor.MiddleCenter, font);

            CreateText(
                "BGMLabel", window.transform, "BGM",
                new Vector2(0.10f, 0.58f), new Vector2(0.28f, 0.68f),
                30, Color.white, TextAnchor.MiddleLeft, font);
            bgmSlider = CreateSlider(
                "BGMSlider", window.transform,
                new Vector2(0.30f, 0.58f), new Vector2(0.78f, 0.68f));
            bgmValue = CreateText(
                "BGMValueText", window.transform, "100%",
                new Vector2(0.80f, 0.58f), new Vector2(0.92f, 0.68f),
                26, Color.white, TextAnchor.MiddleCenter, font);

            CreateText(
                "SFXLabel", window.transform, "SFX",
                new Vector2(0.10f, 0.39f), new Vector2(0.28f, 0.49f),
                30, Color.white, TextAnchor.MiddleLeft, font);
            sfxSlider = CreateSlider(
                "SFXSlider", window.transform,
                new Vector2(0.30f, 0.39f), new Vector2(0.78f, 0.49f));
            sfxValue = CreateText(
                "SFXValueText", window.transform, "100%",
                new Vector2(0.80f, 0.39f), new Vector2(0.92f, 0.49f),
                26, Color.white, TextAnchor.MiddleCenter, font);

            closeButton = CreateButton(
                "CloseButton", window.transform, "닫기",
                new Vector2(0.36f, 0.13f), new Vector2(0.64f, 0.25f), font);
            return panel;
        }

        private static void ConfigureMenuController(
            MainMenuController controller, GameObject mainPanel,
            GameObject settingsPanel, GameObject webQuitMessage,
            Button start, Button settings, Button quit)
        {
            SerializedObject values = new(controller);
            values.FindProperty("gameplaySceneName").stringValue = "Level1";
            values.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            values.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            values.FindProperty("webQuitMessage").objectReferenceValue = webQuitMessage;
            values.FindProperty("startButton").objectReferenceValue = start;
            values.FindProperty("settingsButton").objectReferenceValue = settings;
            values.FindProperty("quitButton").objectReferenceValue = quit;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAudioController(
            AudioSettingsController controller, AudioSource bgm, AudioSource sfx,
            Slider bgmSlider, Slider sfxSlider, Text bgmText, Text sfxText)
        {
            SerializedObject values = new(controller);
            values.FindProperty("bgmSource").objectReferenceValue = bgm;
            SerializedProperty sources = values.FindProperty("sfxSources");
            sources.arraySize = 1;
            sources.GetArrayElementAtIndex(0).objectReferenceValue = sfx;
            values.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
            values.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            values.FindProperty("bgmValueText").objectReferenceValue = bgmText;
            values.FindProperty("sfxValueText").objectReferenceValue = sfxText;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindButtons(
            MainMenuController controller, Button start, Button settings,
            Button quit, Button close)
        {
            Bind(start, controller.StartGame);
            Bind(settings, controller.OpenSettings);
            Bind(quit, controller.QuitGame);
            Bind(close, controller.CloseSettings);
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

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            root.GetComponent<Image>().color = color;
            return root;
        }

        private static GameObject CreateImage(
            string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>(), min, max);
            root.GetComponent<Image>().color = color;
            return root;
        }

        private static Text CreateText(
            string name, Transform parent, string content,
            Vector2 min, Vector2 max, int size, Color color,
            TextAnchor alignment, Font font)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>(), min, max);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
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
            colors.highlightedColor = new Color(0.34f, 0.34f, 0.42f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.13f, 1f);
            button.colors = colors;
            CreateText(
                "Text", root.transform, label, Vector2.zero, Vector2.one,
                30, Color.white, TextAnchor.MiddleCenter, font);
            return button;
        }

        private static Slider CreateSlider(
            string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>(), min, max);

            GameObject background = CreateImage(
                "Background", root.transform,
                new Vector2(0f, 0.38f), new Vector2(1f, 0.62f),
                new Color(0.18f, 0.18f, 0.2f, 1f));
            GameObject fillArea = new("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(),
                new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.62f));
            GameObject fill = CreateImage(
                "Fill", fillArea.transform, Vector2.zero, Vector2.one,
                new Color(0.72f, 0.72f, 0.82f, 1f));

            GameObject handleArea = new("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(),
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
            GameObject handle = CreateImage(
                "Handle", handleArea.transform,
                new Vector2(0f, 0.15f), new Vector2(0.045f, 0.85f), Color.white);

            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            background.GetComponent<Image>().raycastTarget = false;
            return slider;
        }

        private static void Stretch(
            RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ConfigureBuildSettings()
        {
            string[] required = { MainMenuScenePath, Level1ScenePath };
            EditorBuildSettingsScene[] remaining = EditorBuildSettings.scenes
                .Where(scene => !required.Contains(scene.path))
                .ToArray();
            EditorBuildSettings.scenes = required
                .Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(remaining)
                .ToArray();
        }
    }
}
#endif
