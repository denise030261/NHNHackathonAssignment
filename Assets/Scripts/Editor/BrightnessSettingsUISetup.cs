#if UNITY_EDITOR
using System;
using System.Linq;
using NHNHackathon.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class BrightnessSettingsUISetup
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string Level1Path = "Assets/Scenes/Level1.unity";
        private const string RecommendedImagePath =
            "Assets/Art/AlphaRecommandImage.png";
        private const string CurrentImagePath =
            "Assets/Art/AlphaTestImage.png";

        [MenuItem("Tools/NHN Hackathon/UI/Build Brightness Settings")]
        public static void BuildFromMenu()
        {
            if (!CanChangeLoadedScenes())
            {
                Debug.LogWarning(
                    "Save all currently edited scenes before building brightness settings.");
                return;
            }

            BuildAllScenes();
        }

        private static bool CanChangeLoadedScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    return false;
                }
            }
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void BuildAllScenes()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildScene(MainMenuPath, "SettingsPanel");
                BuildScene(Level1Path, "PauseSettingsPanel");
                AssetDatabase.SaveAssets();
                Debug.Log("BRIGHTNESS_SETTINGS_UI_SETUP_COMPLETE");
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }
        }

        private static void BuildScene(string scenePath, string panelName)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Transform panel = FindTransform(scene, panelName)
                ?? throw new InvalidOperationException(
                    $"{panelName} was not found in {scenePath}.");

            Slider bgmSlider = FindComponentByName<Slider>(panel, "BGMSlider");
            Slider sfxSlider = FindComponentByName<Slider>(panel, "SFXSlider");
            Text bgmLabel = FindComponentByName<Text>(panel, "BGMLabel");
            Text sfxLabel = FindComponentByName<Text>(panel, "SFXLabel");
            Text bgmValue = FindComponentByName<Text>(panel, "BGMValueText");
            Text sfxValue = FindComponentByName<Text>(panel, "SFXValueText");
            if (bgmSlider == null || sfxSlider == null || bgmLabel == null
                || sfxLabel == null || bgmValue == null || sfxValue == null)
            {
                throw new InvalidOperationException(
                    $"Audio setting controls are incomplete in {scenePath}.");
            }

            Slider brightnessSlider = FindComponentByName<Slider>(
                panel, "BrightnessSlider");
            if (brightnessSlider == null)
            {
                brightnessSlider = UnityEngine.Object.Instantiate(
                    sfxSlider, sfxSlider.transform.parent);
                brightnessSlider.name = "BrightnessSlider";
                brightnessSlider.onValueChanged = new Slider.SliderEvent();
                MoveToNextRow(
                    bgmSlider.transform as RectTransform,
                    sfxSlider.transform as RectTransform,
                    brightnessSlider.transform as RectTransform);
            }

            Text brightnessLabel = FindComponentByName<Text>(
                panel, "BrightnessLabel");
            if (brightnessLabel == null)
            {
                brightnessLabel = UnityEngine.Object.Instantiate(
                    sfxLabel, sfxLabel.transform.parent);
                brightnessLabel.name = "BrightnessLabel";
                brightnessLabel.text = "화면 밝기";
                MoveToNextRow(
                    bgmLabel.rectTransform, sfxLabel.rectTransform,
                    brightnessLabel.rectTransform);
            }

            Text brightnessValue = FindComponentByName<Text>(
                panel, "BrightnessValueText");
            if (brightnessValue == null)
            {
                brightnessValue = UnityEngine.Object.Instantiate(
                    sfxValue, sfxValue.transform.parent);
                brightnessValue.name = "BrightnessValueText";
                brightnessValue.text = "50%";
                MoveToNextRow(
                    bgmValue.rectTransform, sfxValue.rectTransform,
                    brightnessValue.rectTransform);
            }

            RawImage recommendedPreview = FindComponentByName<RawImage>(
                panel, "RecommendedBrightnessPreview");
            RawImage currentPreview = FindComponentByName<RawImage>(
                panel, "CurrentBrightnessPreview");
            if (recommendedPreview == null || currentPreview == null)
            {
                CreateBrightnessPreview(
                    panel, sfxSlider.transform.parent, sfxLabel,
                    out recommendedPreview, out currentPreview);
            }

            Texture recommendedTexture = AssetDatabase.LoadAssetAtPath<Texture>(
                RecommendedImagePath);
            Texture currentTexture = AssetDatabase.LoadAssetAtPath<Texture>(
                CurrentImagePath);
            recommendedPreview.texture = recommendedTexture;
            currentPreview.texture = currentTexture;

            Text title = FindComponentByName<Text>(panel, "TitleText");
            Button closeButton = FindComponentByName<Button>(panel, "CloseButton");
            if (title == null || closeButton == null)
            {
                throw new InvalidOperationException(
                    $"Settings title or close button is missing in {scenePath}.");
            }

            BuildTabbedLayout(
                panel, title.transform.parent, title, closeButton,
                bgmLabel, bgmSlider, bgmValue, sfxLabel, sfxSlider, sfxValue,
                brightnessLabel, brightnessSlider, brightnessValue,
                recommendedPreview.transform.parent.parent as RectTransform,
                out Button musicButton, out Button lightButton,
                out RectTransform musicContent, out RectTransform lightContent);

            SettingsTabController tabController =
                panel.GetComponent<SettingsTabController>()
                ?? panel.gameObject.AddComponent<SettingsTabController>();
            SerializedObject tabValues = new(tabController);
            tabValues.FindProperty("musicButton").objectReferenceValue = musicButton;
            tabValues.FindProperty("lightButton").objectReferenceValue = lightButton;
            tabValues.FindProperty("musicContent").objectReferenceValue =
                musicContent.gameObject;
            tabValues.FindProperty("lightContent").objectReferenceValue =
                lightContent.gameObject;
            tabValues.ApplyModifiedPropertiesWithoutUndo();

            BrightnessSettingsController brightnessController =
                panel.GetComponent<BrightnessSettingsController>()
                ?? panel.gameObject.AddComponent<BrightnessSettingsController>();
            SerializedObject brightnessValues = new(brightnessController);
            brightnessValues.FindProperty("brightnessSlider").objectReferenceValue =
                brightnessSlider;
            brightnessValues.FindProperty("brightnessValueText").objectReferenceValue =
                brightnessValue;
            brightnessValues.FindProperty("recommendedPreviewImage")
                .objectReferenceValue = recommendedPreview;
            brightnessValues.FindProperty("currentPreviewImage")
                .objectReferenceValue = currentPreview;
            brightnessValues.FindProperty("recommendedPreviewTexture")
                .objectReferenceValue = recommendedTexture;
            brightnessValues.FindProperty("currentPreviewTexture")
                .objectReferenceValue = currentTexture;
            brightnessValues.ApplyModifiedPropertiesWithoutUndo();

            Slider[] allSliders = panel.GetComponentsInChildren<Slider>(true)
                .Where(slider => slider == bgmSlider || slider == sfxSlider
                    || slider == brightnessSlider)
                .ToArray();
            SettingsSliderVisualController visualController =
                panel.GetComponent<SettingsSliderVisualController>()
                ?? panel.gameObject.AddComponent<SettingsSliderVisualController>();
            SerializedObject visualValues = new(visualController);
            SerializedProperty sliderArray = visualValues.FindProperty("sliders");
            sliderArray.arraySize = allSliders.Length;
            for (int index = 0; index < allSliders.Length; index++)
            {
                sliderArray.GetArrayElementAtIndex(index).objectReferenceValue =
                    allSliders[index];
            }
            visualValues.FindProperty("handleSize").floatValue = 34f;
            visualValues.ApplyModifiedPropertiesWithoutUndo();
            visualController.Apply();

            EditorUtility.SetDirty(brightnessSlider);
            EditorUtility.SetDirty(brightnessLabel);
            EditorUtility.SetDirty(brightnessValue);
            EditorUtility.SetDirty(recommendedPreview);
            EditorUtility.SetDirty(currentPreview);
            EditorUtility.SetDirty(tabController);
            EditorUtility.SetDirty(brightnessController);
            EditorUtility.SetDirty(visualController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void BuildTabbedLayout(
            Transform panel, Transform container, Text title, Button closeButton,
            Text bgmLabel, Slider bgmSlider, Text bgmValue,
            Text sfxLabel, Slider sfxSlider, Text sfxValue,
            Text brightnessLabel, Slider brightnessSlider, Text brightnessValue,
            RectTransform previewRoot, out Button musicButton,
            out Button lightButton, out RectTransform musicContent,
            out RectTransform lightContent)
        {
            RectTransform panelRect = panel as RectTransform;
            RectTransform containerRect = container as RectTransform;
            if (container == panel)
            {
                SetAnchors(panelRect, new Vector2(0.18f, 0.08f),
                    new Vector2(0.82f, 0.92f));
            }
            else
            {
                SetAnchors(containerRect, new Vector2(0.12f, 0.08f),
                    new Vector2(0.88f, 0.92f));
            }

            title.text = "환경설정";
            SetAnchors(title.rectTransform, new Vector2(0.08f, 0.87f),
                new Vector2(0.92f, 0.98f));
            SetAnchors(closeButton.transform as RectTransform,
                new Vector2(0.36f, 0.04f), new Vector2(0.64f, 0.14f));

            RectTransform contentFrame = FindChildRect(
                panel, "SettingsTabContentFrame");
            if (contentFrame == null)
            {
                GameObject frameObject = new(
                    "SettingsTabContentFrame", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                frameObject.layer = panel.gameObject.layer;
                contentFrame = frameObject.GetComponent<RectTransform>();
                contentFrame.SetParent(container, false);
                frameObject.GetComponent<Image>().color =
                    new Color(0.035f, 0.035f, 0.045f, 0.96f);
            }
            SetAnchors(contentFrame, new Vector2(0.07f, 0.20f),
                new Vector2(0.93f, 0.72f));

            musicContent = GetOrCreateContent(
                panel, contentFrame, "MusicContent");
            lightContent = GetOrCreateContent(
                panel, contentFrame, "LightContent");

            musicButton = GetOrCreateTabButton(
                panel, container, closeButton, "MusicTabButton", "Music",
                new Vector2(0.14f, 0.75f), new Vector2(0.46f, 0.85f));
            lightButton = GetOrCreateTabButton(
                panel, container, closeButton, "LightTabButton", "Light",
                new Vector2(0.54f, 0.75f), new Vector2(0.86f, 0.85f));

            MoveControl(bgmLabel.rectTransform, musicContent,
                new Vector2(0.08f, 0.62f), new Vector2(0.26f, 0.78f));
            MoveControl(bgmSlider.transform as RectTransform, musicContent,
                new Vector2(0.28f, 0.62f), new Vector2(0.78f, 0.78f));
            MoveControl(bgmValue.rectTransform, musicContent,
                new Vector2(0.80f, 0.62f), new Vector2(0.94f, 0.78f));
            MoveControl(sfxLabel.rectTransform, musicContent,
                new Vector2(0.08f, 0.28f), new Vector2(0.26f, 0.44f));
            MoveControl(sfxSlider.transform as RectTransform, musicContent,
                new Vector2(0.28f, 0.28f), new Vector2(0.78f, 0.44f));
            MoveControl(sfxValue.rectTransform, musicContent,
                new Vector2(0.80f, 0.28f), new Vector2(0.94f, 0.44f));

            MoveControl(previewRoot, lightContent,
                new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.98f));
            MoveControl(brightnessLabel.rectTransform, lightContent,
                new Vector2(0.08f, 0.08f), new Vector2(0.26f, 0.24f));
            MoveControl(brightnessSlider.transform as RectTransform, lightContent,
                new Vector2(0.28f, 0.08f), new Vector2(0.78f, 0.24f));
            MoveControl(brightnessValue.rectTransform, lightContent,
                new Vector2(0.80f, 0.08f), new Vector2(0.94f, 0.24f));

            musicContent.gameObject.SetActive(true);
            lightContent.gameObject.SetActive(false);
        }

        private static RectTransform GetOrCreateContent(
            Transform panel, RectTransform parent, string name)
        {
            RectTransform content = FindChildRect(panel, name);
            if (content == null)
            {
                GameObject contentObject = new(name, typeof(RectTransform));
                contentObject.layer = panel.gameObject.layer;
                content = contentObject.GetComponent<RectTransform>();
                content.SetParent(parent, false);
            }
            SetAnchors(content, Vector2.zero, Vector2.one);
            return content;
        }

        private static Button GetOrCreateTabButton(
            Transform panel, Transform parent, Button template,
            string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            Button button = FindComponentByName<Button>(panel, name);
            if (button == null)
            {
                button = UnityEngine.Object.Instantiate(template, parent);
                button.name = name;
                button.onClick = new Button.ButtonClickedEvent();
            }

            Text buttonLabel = button.GetComponentInChildren<Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = label;
            }
            SetAnchors(button.transform as RectTransform, anchorMin, anchorMax);
            return button;
        }

        private static void MoveControl(
            RectTransform control, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            control.SetParent(parent, false);
            SetAnchors(control, anchorMin, anchorMax);
        }

        private static RectTransform FindChildRect(
            Transform root, string objectName)
        {
            Transform result = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == objectName);
            return result as RectTransform;
        }

        private static void CreateBrightnessPreview(
            Transform panel, Transform parent, Text labelTemplate,
            out RawImage recommendedPreview, out RawImage currentPreview)
        {
            GameObject rootObject = new(
                "BrightnessPreviewRoot", typeof(RectTransform));
            rootObject.layer = panel.gameObject.layer;
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            SetAnchors(root, new Vector2(0.16f, 0.71f),
                new Vector2(0.84f, 0.97f));

            CreatePreviewLabel(
                labelTemplate, root, "RecommendedBrightnessLabel", "권장 밝기",
                new Vector2(0f, 0.84f), new Vector2(0.47f, 1f));
            CreatePreviewLabel(
                labelTemplate, root, "CurrentBrightnessLabel", "현재 밝기",
                new Vector2(0.53f, 0.84f), new Vector2(1f, 1f));

            recommendedPreview = CreatePreviewImage(
                root, "RecommendedBrightnessFrame",
                "RecommendedBrightnessPreview", new Vector2(0f, 0f),
                new Vector2(0.47f, 0.82f));
            currentPreview = CreatePreviewImage(
                root, "CurrentBrightnessFrame", "CurrentBrightnessPreview",
                new Vector2(0.53f, 0f), new Vector2(1f, 0.82f));
        }

        private static void CreatePreviewLabel(
            Text template, RectTransform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Text label = UnityEngine.Object.Instantiate(template, parent);
            label.name = name;
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            SetAnchors(label.rectTransform, anchorMin, anchorMax);
        }

        private static RawImage CreatePreviewImage(
            RectTransform parent, string frameName, string imageName,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject frameObject = new(
                frameName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            frameObject.layer = parent.gameObject.layer;
            RectTransform frame = frameObject.GetComponent<RectTransform>();
            frame.SetParent(parent, false);
            SetAnchors(frame, anchorMin, anchorMax);
            frameObject.GetComponent<Image>().color = Color.black;

            GameObject imageObject = new(
                imageName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RawImage), typeof(AspectRatioFitter));
            imageObject.layer = parent.gameObject.layer;
            RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
            imageTransform.SetParent(frame, false);
            SetAnchors(imageTransform, Vector2.zero, Vector2.one);
            imageTransform.offsetMin = new Vector2(4f, 4f);
            imageTransform.offsetMax = new Vector2(-4f, -4f);

            AspectRatioFitter fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1210f / 1060f;
            return imageObject.GetComponent<RawImage>();
        }

        private static void SetAnchors(
            RectTransform transform, Vector2 anchorMin, Vector2 anchorMax)
        {
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
        }

        private static void MoveToNextRow(
            RectTransform firstRow, RectTransform secondRow,
            RectTransform target)
        {
            Vector2 anchorShift = secondRow.anchorMin - firstRow.anchorMin;
            target.anchorMin = secondRow.anchorMin + anchorShift;
            target.anchorMax = secondRow.anchorMax
                + (secondRow.anchorMax - firstRow.anchorMax);
            target.anchoredPosition = secondRow.anchoredPosition;
            target.sizeDelta = secondRow.sizeDelta;
        }

        private static T FindComponentByName<T>(Transform root, string objectName)
            where T : Component
        {
            return root.GetComponentsInChildren<T>(true)
                .FirstOrDefault(component => component.gameObject.name == objectName);
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform result = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == objectName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
#endif
