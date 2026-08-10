#if UNITY_EDITOR
using System.Linq;
using System.IO;
using NHNHackathon.ExitSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace NHNHackathon.EditorTools
{
    public static class Level1EndingCreditsSetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string VideoPath = "Assets/Art/Ending.mp4";
        private const string RenderTexturePath = "Assets/Art/EndingCredits.renderTexture";
        private const string StreamingVideoPath = "Assets/StreamingAssets/Ending.mp4";
        private const string AutoRunKey = "NHN.Level1EndingCreditsSetup.20260810.5";

        [InitializeOnLoadMethod]
        private static void ScheduleOnce()
        {
            if (SessionState.GetBool(AutoRunKey, false))
            {
                return;
            }

            SessionState.SetBool(AutoRunKey, true);
            EditorApplication.delayCall += TryAutoBuild;
        }

        [MenuItem("NHN Hackathon/Setup/Level1 Ending Credits")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameSuccessController successController =
                Object.FindFirstObjectByType<GameSuccessController>(FindObjectsInactive.Include);
            Canvas canvas = FindGameCanvas();
            if (successController == null || canvas == null || !File.Exists(VideoPath))
            {
                throw new System.InvalidOperationException(
                    "Level1 ending setup requires GameSuccessController, UI Canvas and Assets/Art/Ending.mp4.");
            }

            CopyVideoToStreamingAssets();
            RenderTexture renderTexture = GetOrCreateRenderTexture();
            Transform previous = canvas.transform.Find("EndingCreditsUI");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            GameObject endingRoot = CreateUIObject("EndingCreditsUI", canvas.transform);
            Stretch(endingRoot.GetComponent<RectTransform>());
            endingRoot.transform.SetAsLastSibling();

            RawImage videoImage = CreateUIObject("VideoBackground", endingRoot.transform)
                .AddComponent<RawImage>();
            videoImage.texture = renderTexture;
            videoImage.color = Color.white;
            videoImage.raycastTarget = false;
            Stretch(videoImage.rectTransform);
            AspectRatioFitter aspect = videoImage.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 16f / 9f;

            Image shade = CreateUIObject("CreditsShade", endingRoot.transform).AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.38f);
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);

            GameObject viewportObject = CreateUIObject("CreditsViewport", endingRoot.transform);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportObject.AddComponent<RectMask2D>();

            Text creditsText = CreateText(
                "CreditsText", viewportObject.transform, 40, TextAnchor.MiddleCenter);
            creditsText.supportRichText = true;
            creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.lineSpacing = 1.3f;
            creditsText.text =
                "<size=64>THE END</size>\n\n\n"
                + "<size=48>NHN HACKATHON</size>\n\n\n"
                + "기획\n\n\n"
                + "프로그래밍\n\n\n"
                + "3D 아트\n\n\n"
                + "애니메이션\n\n\n"
                + "사운드\n\n\n\n\n"
                + "플레이해 주셔서 감사합니다.";
            RectTransform creditsRect = creditsText.rectTransform;
            creditsRect.anchorMin = creditsRect.anchorMax = new Vector2(0.5f, 0.5f);
            creditsRect.pivot = new Vector2(0.5f, 0.5f);
            creditsRect.sizeDelta = new Vector2(1200f, 1200f);
            ContentSizeFitter fitter = creditsText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text guide = CreateText(
                "HoldSpaceGuide", endingRoot.transform, 24, TextAnchor.MiddleCenter);
            guide.text = "Space를 누르고 있으면 크레딧이 빨라집니다.";
            guide.color = new Color(1f, 1f, 1f, 0.8f);
            guide.raycastTarget = false;
            SetRect(guide.rectTransform, new Vector2(0f, 42f), new Vector2(900f, 44f));
            guide.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            guide.rectTransform.anchorMax = new Vector2(0.5f, 0f);

            Image fade = CreateUIObject("FadeImage", endingRoot.transform).AddComponent<Image>();
            fade.color = Color.black;
            fade.raycastTarget = true;
            Stretch(fade.rectTransform);

            GameObject previousSystem = GameObject.Find("EndingCreditsSystem");
            if (previousSystem != null)
            {
                Object.DestroyImmediate(previousSystem);
            }

            GameObject host = new("EndingCreditsSystem");
            EndingCreditsController endingController = host.AddComponent<EndingCreditsController>();
            VideoPlayer videoPlayer = host.AddComponent<VideoPlayer>();
            AudioSource videoAudio = FindOrCreateEndingAudio(host);

            ConfigureVideo(videoPlayer, videoAudio, renderTexture);
            ConfigureEndingController(
                endingController, endingRoot, viewport, creditsRect,
                fade, videoPlayer, videoAudio);

            SerializedObject successValues = new(successController);
            successValues.FindProperty("endingCreditsController").objectReferenceValue = endingController;
            GameObject legacySuccessUI = successValues.FindProperty("gameSuccessUI")
                .objectReferenceValue as GameObject;
            successValues.ApplyModifiedPropertiesWithoutUndo();
            if (legacySuccessUI != null)
            {
                legacySuccessUI.SetActive(false);
            }

            endingRoot.SetActive(false);
            EditorUtility.SetDirty(successController);
            EditorUtility.SetDirty(endingController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_ENDING_CREDITS_COMPLETE: Ending.mp4 + scrolling credits + Space acceleration.");
        }

        private static void TryAutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || SceneManager.GetActiveScene().path != ScenePath)
            {
                return;
            }

            GameSuccessController success =
                Object.FindFirstObjectByType<GameSuccessController>(FindObjectsInactive.Include);
            Canvas canvas = FindGameCanvas();
            EndingCreditsController ending =
                Object.FindFirstObjectByType<EndingCreditsController>(FindObjectsInactive.Include);
            bool isConfigured = success != null
                && ending != null
                && ending.GetComponent<VideoPlayer>() != null
                && canvas != null
                && canvas.transform.Find("EndingCreditsUI") != null;
            if (isConfigured)
            {
                return;
            }

            try
            {
                Build();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void ConfigureEndingController(
            EndingCreditsController controller,
            GameObject root,
            RectTransform viewport,
            RectTransform credits,
            Image fade,
            VideoPlayer player,
            AudioSource audio)
        {
            SerializedObject values = new(controller);
            values.FindProperty("endingRoot").objectReferenceValue = root;
            values.FindProperty("creditsViewport").objectReferenceValue = viewport;
            values.FindProperty("creditsContent").objectReferenceValue = credits;
            values.FindProperty("fadeImage").objectReferenceValue = fade;
            values.FindProperty("videoPlayer").objectReferenceValue = player;
            values.FindProperty("videoAudioSource").objectReferenceValue = audio;
            values.FindProperty("streamingVideoFileName").stringValue = "Ending.mp4";
            values.FindProperty("prepareTimeout").floatValue = 10f;
            values.FindProperty("scrollSpeed").floatValue = 70f;
            values.FindProperty("holdSpaceSpeedMultiplier").floatValue = 4f;
            values.FindProperty("startDelay").floatValue = 0.75f;
            values.FindProperty("screenPadding").floatValue = 80f;
            values.FindProperty("fadeInDuration").floatValue = 1f;
            values.FindProperty("fadeOutDuration").floatValue = 1f;
            values.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureVideo(
            VideoPlayer player,
            AudioSource audio,
            RenderTexture texture)
        {
            player.source = VideoSource.Url;
            player.clip = null;
            player.url = string.Empty;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = texture;
            player.playOnAwake = false;
            player.isLooping = true;
            player.waitForFirstFrame = true;
            player.skipOnDrop = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, audio);
        }

        private static void CopyVideoToStreamingAssets()
        {
            string directory = Path.GetDirectoryName(StreamingVideoPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(StreamingVideoPath))
            {
                FileUtil.ReplaceFile(VideoPath, StreamingVideoPath);
            }
            else
            {
                FileUtil.CopyFileOrDirectory(VideoPath, StreamingVideoPath);
            }
            AssetDatabase.ImportAsset(StreamingVideoPath, ImportAssetOptions.ForceUpdate);
        }

        private static AudioSource FindOrCreateEndingAudio(GameObject host)
        {
            AudioSource[] sources = host.GetComponents<AudioSource>();
            AudioSource audio = sources.FirstOrDefault(
                value => value != null && value.clip == null && !value.playOnAwake);
            audio ??= host.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0f;
            audio.dopplerLevel = 0f;
            audio.ignoreListenerPause = true;
            return audio;
        }

        private static RenderTexture GetOrCreateRenderTexture()
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture != null)
            {
                return texture;
            }

            texture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = "EndingCredits",
                useMipMap = false,
                autoGenerateMips = false
            };
            AssetDatabase.CreateAsset(texture, RenderTexturePath);
            return texture;
        }

        private static Canvas FindGameCanvas()
        {
            return Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(value => value.transform.root.name == "UI")
                ?? Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject value = new(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            value.layer = parent.gameObject.layer;
            return value;
        }

        private static Text CreateText(
            string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            Text value = CreateUIObject(name, parent).AddComponent<Text>();
            value.font = ProjectFontProvider.LoadRegular();
            value.fontSize = fontSize;
            value.alignment = alignment;
            value.color = Color.white;
            value.raycastTarget = false;
            return value;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
#endif
