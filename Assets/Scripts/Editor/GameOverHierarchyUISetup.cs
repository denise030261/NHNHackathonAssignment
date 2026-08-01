#if UNITY_EDITOR
using NHNHackathon.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class GameOverHierarchyUISetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";

        [MenuItem("Tools/NHN Hackathon/UI/Rebuild Game Over Hierarchy UI")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameOverController controller = Object.FindAnyObjectByType<GameOverController>();
            if (controller == null)
            {
                throw new System.InvalidOperationException(
                    $"No GameOverController was found in {ScenePath}.");
            }

            Transform existing = controller.transform.Find("GameOverUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject uiRoot = CreateUI(controller.transform, out Text label);
            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("gameOverUI").objectReferenceValue = uiRoot;
            settings.FindProperty("gameOverText").objectReferenceValue = label;
            settings.ApplyModifiedPropertiesWithoutUndo();
            uiRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Game Over Canvas UI created under GameOverSystem/GameOverUI.");
        }

        private static GameObject CreateUI(Transform parent, out Text label)
        {
            GameObject root = new GameObject("GameOverUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(root.transform, false);
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            GameObject textObject = new GameObject("GameOverText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(overlay.transform, false);
            Stretch(textObject.GetComponent<RectTransform>());
            label = textObject.GetComponent<Text>();
            label.text = "GAME OVER";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 72;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.red;
            return root;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
