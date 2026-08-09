#if UNITY_EDITOR
using System.IO;
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.Input;
using NHNHackathon.Inspection;
using NHNHackathon.Interaction;
using NHNHackathon.Inventory;
using NHNHackathon.Items;
using NHNHackathon.LightSystem;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class DoYoungPracticeInventorySetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";

        [MenuItem("Tools/NHN Hackathon/Rebuild DoYoung Inventory")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("InventorySystem");

            GameObject player = GameObject.Find("Player");
            ItemInspectionController inspectionController =
                Object.FindAnyObjectByType<ItemInspectionController>();
            if (player == null || inspectionController == null)
            {
                throw new System.InvalidOperationException(
                    "Build the player and item inspection systems before the inventory.");
            }

            PlayerItemInventory itemInventory =
                player.GetComponent<PlayerItemInventory>() ?? player.AddComponent<PlayerItemInventory>();

            GameObject root = new GameObject("InventorySystem");
            InspectionControlLock controlLock = root.AddComponent<InspectionControlLock>();
            ItemPreviewRenderer previewRenderer = root.AddComponent<ItemPreviewRenderer>();
            InventoryController controller = root.AddComponent<InventoryController>();

            CreatePreviewCamera(root.transform, previewRenderer);
            CreateInventoryCanvas(
                root.transform, controller, previewRenderer,
                itemInventory, inspectionController);
            ConfigurePrototypeIcons();
            ConfigureControlLock(controlLock, player);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DoYoungPracticeScene inventory setup completed.");
        }

        private static void CreatePreviewCamera(
            Transform parent, ItemPreviewRenderer previewRenderer)
        {
            GameObject cameraObject = new GameObject("InventoryPreviewCamera");
            cameraObject.transform.SetParent(parent, false);
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = true;
            previewCamera.fieldOfView = 35f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 20f;
            cameraObject.AddComponent<Light>().type = LightType.Directional;

            SerializedObject settings = new SerializedObject(previewRenderer);
            settings.FindProperty("previewCamera").objectReferenceValue = previewCamera;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateInventoryCanvas(
            Transform parent,
            InventoryController controller,
            ItemPreviewRenderer previewRenderer,
            PlayerItemInventory inventory,
            ItemInspectionController inspectionController)
        {
            Font font = ProjectFontProvider.LoadRegular();
            GameObject canvasObject = new GameObject(
                "InventoryCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateImage(
                "Background", canvasObject.transform, Vector2.zero, Vector2.one,
                new Color(0f, 0f, 0f, 0.82f));
            Image window = CreateImage(
                "InventoryWindow", canvasObject.transform,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                new Color(0.075f, 0.075f, 0.085f, 0.98f));

            GameObject header = CreateRectObject(
                "Header", window.transform,
                new Vector2(0.035f, 0.87f), new Vector2(0.965f, 0.97f));
            Text headerText = CreateText(
                "TitleText", header.transform,
                Vector2.zero, new Vector2(0.8f, 1f),
                "\uC778\uBCA4\uD1A0\uB9AC", 36, Color.white, font);
            headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.MiddleLeft;
            Button closeButton = CreateButton(
                "CloseButton", header.transform,
                new Vector2(0.88f, 0.12f), Vector2.one,
                "\uB2EB\uAE30", font);

            Image listPanel = CreateImage(
                "ItemListPanel", window.transform,
                new Vector2(0.035f, 0.07f), new Vector2(0.36f, 0.84f),
                new Color(0.11f, 0.11f, 0.125f, 1f));
            ScrollRect scrollRect = listPanel.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            Image viewport = CreateImage(
                "Viewport", listPanel.transform,
                new Vector2(0.035f, 0.025f), new Vector2(0.965f, 0.975f),
                new Color(1f, 1f, 1f, 0.01f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            GameObject content = CreateRectObject(
                "Content", viewport.transform,
                new Vector2(0f, 1f), Vector2.one);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            GridLayoutGroup layout = content.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.cellSize = new Vector2(128f, 128f);
            layout.spacing = new Vector2(14f, 14f);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = contentRect;

            Button itemTemplate = CreateIconSlot(content.transform);
            itemTemplate.gameObject.SetActive(false);

            Image detailPanel = CreateImage(
                "ItemDetailPanel", window.transform,
                new Vector2(0.39f, 0.07f), new Vector2(0.965f, 0.84f),
                new Color(0.095f, 0.095f, 0.11f, 1f));

            GameObject previewPanel = CreateRectObject(
                "ItemPreviewPanel", detailPanel.transform,
                new Vector2(0.055f, 0.42f), new Vector2(0.945f, 0.96f));
            RawImage preview = previewPanel.AddComponent<RawImage>();
            ItemPreviewDragHandler dragHandler =
                previewPanel.AddComponent<ItemPreviewDragHandler>();
            SerializedObject dragSettings = new SerializedObject(dragHandler);
            dragSettings.FindProperty("previewRenderer").objectReferenceValue = previewRenderer;
            dragSettings.ApplyModifiedPropertiesWithoutUndo();

            Text itemName = CreateText(
                "ItemNameText", detailPanel.transform,
                new Vector2(0.055f, 0.32f), new Vector2(0.945f, 0.41f),
                string.Empty, 32, Color.white, font);
            itemName.fontStyle = FontStyle.Bold;
            itemName.alignment = TextAnchor.MiddleLeft;

            Text description = CreateText(
                "ItemDescriptionText", detailPanel.transform,
                new Vector2(0.055f, 0.12f), new Vector2(0.945f, 0.31f),
                string.Empty, 21, new Color(0.86f, 0.86f, 0.86f), font);
            description.alignment = TextAnchor.UpperLeft;

            Button readButton = CreateButton(
                "ReadButton", detailPanel.transform,
                new Vector2(0.55f, 0.025f), new Vector2(0.945f, 0.105f),
                "\uC790\uC138\uD788 \uC77D\uAE30", font);

            UnityEventTools.AddPersistentListener(
                closeButton.onClick, controller.CloseInventory);
            UnityEventTools.AddPersistentListener(
                readButton.onClick, controller.ReadSelectedPaper);

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("inventory").objectReferenceValue = inventory;
            settings.FindProperty("inspectionController").objectReferenceValue = inspectionController;
            settings.FindProperty("canvasRoot").objectReferenceValue = canvasObject;
            settings.FindProperty("itemListContent").objectReferenceValue = content.transform;
            settings.FindProperty("itemButtonTemplate").objectReferenceValue = itemTemplate;
            settings.FindProperty("detailPanel").objectReferenceValue = detailPanel.gameObject;
            settings.FindProperty("previewImage").objectReferenceValue = preview;
            settings.FindProperty("itemNameText").objectReferenceValue = itemName;
            settings.FindProperty("itemDescriptionText").objectReferenceValue = description;
            settings.FindProperty("readButton").objectReferenceValue = readButton;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateIconSlot(Transform parent)
        {
            Image background = CreateImage(
                "ItemSlotTemplate", parent,
                Vector2.zero, Vector2.one,
                new Color(0.16f, 0.16f, 0.18f, 1f));
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            Outline outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.72f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            Image icon = CreateImage(
                "ItemIcon", background.transform,
                new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.90f),
                Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return button;
        }

        private static void ConfigurePrototypeIcons()
        {
            const string iconDirectory = "Assets/Art/InventoryIcons";
            EnsureDirectory(iconDirectory);

            Sprite keepsakeIcon = CreatePrototypeIcon(
                $"{iconDirectory}/ClockworkKeepsakeIcon.asset",
                ItemType.General);
            Sprite paperIcon = CreatePrototypeIcon(
                $"{iconDirectory}/FactoryMemoIcon.asset",
                ItemType.Paper);
            Sprite keyIcon = CreatePrototypeIcon(
                $"{iconDirectory}/KeyIcon.asset",
                ItemType.Key);

            AssignIcon("Assets/Data/Items/ClockworkKeepsake.asset", keepsakeIcon);
            AssignIcon("Assets/Data/Items/FactoryMemo.asset", paperIcon);
            AssignIcon("Assets/Data/Items/Key_01.asset", keyIcon);
            AssignIcon("Assets/Data/Items/Key_02.asset", keyIcon);
            AssignIcon("Assets/Data/Items/Key_03.asset", keyIcon);
        }

        private static Sprite CreatePrototypeIcon(string path, ItemType type)
        {
            foreach (Object representation in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (representation is Sprite existingSprite)
                {
                    return existingSprite;
                }
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                const int size = 128;
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    filterMode = FilterMode.Point
                };
                Color[] pixels = new Color[size * size];
                for (int index = 0; index < pixels.Length; index++)
                {
                    pixels[index] = Color.clear;
                }

                DrawIconPixels(pixels, size, type);
                texture.SetPixels(pixels);
                texture.Apply();
                AssetDatabase.CreateAsset(texture, path);
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = $"{texture.name}_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            EditorUtility.SetDirty(texture);
            return sprite;
        }

        private static void DrawIconPixels(Color[] pixels, int size, ItemType type)
        {
            Color gold = new Color(0.95f, 0.65f, 0.12f, 1f);
            Color paper = new Color(0.88f, 0.82f, 0.62f, 1f);
            Color ink = new Color(0.25f, 0.2f, 0.14f, 1f);
            Color red = new Color(0.67f, 0.16f, 0.12f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool fill = false;
                    Color color = Color.clear;
                    if (type == ItemType.Key)
                    {
                        int dx = x - 38;
                        int dy = y - 82;
                        bool ring = dx * dx + dy * dy < 24 * 24
                                    && dx * dx + dy * dy > 12 * 12;
                        bool shaft = x >= 48 && x <= 100 && y >= 76 && y <= 88;
                        bool tooth = x >= 88 && x <= 100 && y >= 62 && y <= 78;
                        fill = ring || shaft || tooth;
                        color = gold;
                    }
                    else if (type == ItemType.Paper)
                    {
                        fill = x >= 24 && x <= 104 && y >= 16 && y <= 112;
                        color = paper;
                        if (fill && y >= 38 && y <= 92 && y % 14 < 3
                            && x >= 38 && x <= 90)
                        {
                            color = ink;
                        }
                    }
                    else
                    {
                        int dx = x - 64;
                        int dy = y - 64;
                        int radiusSquared = dx * dx + dy * dy;
                        fill = radiusSquared <= 42 * 42 && radiusSquared >= 18 * 18;
                        color = red;
                    }

                    if (fill)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        private static void AssignIcon(string itemPath, Sprite icon)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            if (item == null)
            {
                return;
            }

            SerializedObject settings = new SerializedObject(item);
            settings.FindProperty("inventoryIcon").objectReferenceValue = icon;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void ConfigureControlLock(
            InspectionControlLock controlLock, GameObject player)
        {
            Behaviour[] controls =
            {
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<PlayerCameraController>(),
                player.GetComponent<PlayerDanceInput>(),
                player.GetComponent<PlayerCursorController>(),
                player.GetComponentInChildren<PlayerFlashlightController>(true),
                player.GetComponent<PlayerInteractor>()
            };

            SerializedObject settings = new SerializedObject(controlLock);
            SerializedProperty controlled = settings.FindProperty("controlledBehaviours");
            controlled.arraySize = controls.Length;
            for (int index = 0; index < controls.Length; index++)
            {
                controlled.GetArrayElementAtIndex(index).objectReferenceValue = controls[index];
            }
            settings.FindProperty("pauseWorldDuringInspection").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateRectObject(
            string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return result;
        }

        private static Image CreateImage(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject result = CreateRectObject(name, parent, anchorMin, anchorMax);
            Image image = result.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            string text, int fontSize, Color color, Font font)
        {
            GameObject result = CreateRectObject(name, parent, anchorMin, anchorMax);
            Text label = result.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            string text, Font font)
        {
            Image image = CreateImage(
                name, parent, anchorMin, anchorMax,
                new Color(0.17f, 0.17f, 0.19f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text label = CreateText(
                "Text", image.transform, Vector2.zero, Vector2.one,
                text, 20, Color.white, font);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static void DeleteNamedObject(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void EnsureDirectory(string directory)
        {
            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string parent = Path.GetDirectoryName(directory)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureDirectory(parent);
            }
            AssetDatabase.CreateFolder(parent, Path.GetFileName(directory));
        }
    }
}
#endif
