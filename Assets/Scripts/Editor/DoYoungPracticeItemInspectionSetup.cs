#if UNITY_EDITOR
using System.IO;
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.Input;
using NHNHackathon.Inspection;
using NHNHackathon.Interaction;
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
    public static class DoYoungPracticeItemInspectionSetup
    {
        private const string ScenePath = "Assets/Scenes/DoYoungPracticeScene.unity";
        private const string DataDirectory = "Assets/Data/Items";
        private const string PrefabDirectory = "Assets/Prefabs/Items";
        private const string MaterialDirectory = "Assets/Art/Materials";
        private const string BootstrapAssetPath = DataDirectory + "/FactoryMemo.asset";
        private static int framesUntilPlay;

        [InitializeOnLoadMethod]
        private static void RunFirstSetupAfterCompilation()
        {
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(BootstrapAssetPath) != null)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling
                    || AssetDatabase.LoadAssetAtPath<ItemDefinition>(BootstrapAssetPath) != null)
                {
                    return;
                }

                Build();
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("Tools/NHN Hackathon/Rebuild DoYoung Item Inspection")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteNamedObject("ItemInspectionSystem");
            DeleteNamedObject("InspectableItems");

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new System.InvalidOperationException(
                    "DoYoungPracticeScene requires a Player object.");
            }

            PlayerItemInventory inventory =
                player.GetComponent<PlayerItemInventory>() ?? player.AddComponent<PlayerItemInventory>();
            PlayerInteractor interactor =
                player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();

            CreateInspectionSystem(player, interactor);

            EnsureDirectory(DataDirectory);
            EnsureDirectory(PrefabDirectory);
            EnsureDirectory(MaterialDirectory);

            GameObject keepsakePreview = CreatePreviewPrefab(
                "ClockworkKeepsake",
                PrimitiveType.Cylinder,
                new Vector3(0.75f, 0.18f, 0.75f),
                new Color(0.65f, 0.17f, 0.12f));
            GameObject paperPreview = CreatePaperPreviewPrefab();

            ItemDefinition keepsake = CreateItemDefinition(
                $"{DataDirectory}/ClockworkKeepsake.asset",
                "Clockwork_Keepsake",
                "망가진 태엽 부품",
                ItemType.General,
                "오래된 자동인형의 가슴에서 떨어져 나온 태엽 부품이다.\n표면에 알아보기 힘든 일련번호가 새겨져 있다.",
                keepsakePreview,
                1.15f);
            ItemDefinition paper = CreateItemDefinition(
                $"{DataDirectory}/FactoryMemo.asset",
                "Factory_Memo",
                "공장 관리 기록",
                ItemType.Paper,
                "가장자리가 낡은 공장 관리자의 기록지다.",
                paperPreview,
                1.1f);
            SetPaperPages(paper);
            ConfigureExistingKeys(keepsakePreview);

            Transform itemsRoot = new GameObject("InspectableItems").transform;
            Vector3 origin = player.transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f)
            {
                forward = Vector3.forward;
            }
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            CreateWorldItem(
                "Inspectable_ClockworkKeepsake",
                keepsake,
                keepsakePreview,
                origin + forward * 2.1f + right * 0.65f,
                itemsRoot);
            CreateWorldItem(
                "Inspectable_FactoryMemo",
                paper,
                paperPreview,
                origin + forward * 2.1f - right * 0.65f,
                itemsRoot);

            EditorUtility.SetDirty(inventory);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DoYoungPracticeScene item inspection setup completed.");
        }

        public static void OpenAndPlay()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            framesUntilPlay = 30;
            EditorApplication.update -= WaitUntilReadyAndPlay;
            EditorApplication.update += WaitUntilReadyAndPlay;
        }

        private static void WaitUntilReadyAndPlay()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            framesUntilPlay--;
            if (framesUntilPlay > 0)
            {
                return;
            }

            EditorApplication.update -= WaitUntilReadyAndPlay;
            EditorApplication.isPlaying = true;
        }

        private static void CreateInspectionSystem(GameObject player, PlayerInteractor interactor)
        {
            GameObject root = new GameObject("ItemInspectionSystem");
            InspectionControlLock controlLock = root.AddComponent<InspectionControlLock>();
            ItemPreviewRenderer previewRenderer = root.AddComponent<ItemPreviewRenderer>();
            ItemInspectionController controller = root.AddComponent<ItemInspectionController>();

            GameObject cameraObject = new GameObject("ItemPreviewCamera");
            cameraObject.transform.SetParent(root.transform, false);
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = true;
            previewCamera.fieldOfView = 35f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 20f;
            cameraObject.AddComponent<Light>().type = LightType.Directional;

            SerializedObject previewSettings = new SerializedObject(previewRenderer);
            previewSettings.FindProperty("previewCamera").objectReferenceValue = previewCamera;
            previewSettings.ApplyModifiedPropertiesWithoutUndo();

            CreateCanvasInterface(root.transform, controller, previewRenderer);
            EnsureEventSystem();

            Behaviour[] controls =
            {
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<PlayerCameraController>(),
                player.GetComponent<PlayerDanceInput>(),
                player.GetComponent<PlayerCursorController>(),
                player.GetComponentInChildren<PlayerFlashlightController>(true),
                interactor
            };
            SerializedObject lockSettings = new SerializedObject(controlLock);
            SerializedProperty controlled = lockSettings.FindProperty("controlledBehaviours");
            controlled.arraySize = controls.Length;
            for (int index = 0; index < controls.Length; index++)
            {
                controlled.GetArrayElementAtIndex(index).objectReferenceValue = controls[index];
            }
            lockSettings.FindProperty("pauseWorldDuringInspection").boolValue = true;
            lockSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCanvasInterface(
            Transform parent,
            ItemInspectionController controller,
            ItemPreviewRenderer previewRenderer)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new GameObject(
                "ItemInspectionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage(
                "BlackBackground", canvasObject.transform,
                Vector2.zero, Vector2.one,
                new Color(0f, 0f, 0f, 0.96f));
            background.raycastTarget = true;

            GameObject overview = CreateRectObject(
                "ItemOverview", canvasObject.transform, Vector2.zero, Vector2.one);

            GameObject previewPanel = CreateRectObject(
                "PreviewPanel", overview.transform,
                new Vector2(0.035f, 0.075f), new Vector2(0.565f, 0.925f));
            RawImage previewImage = previewPanel.AddComponent<RawImage>();
            previewImage.color = Color.white;
            previewImage.raycastTarget = true;
            ItemPreviewDragHandler dragHandler =
                previewPanel.AddComponent<ItemPreviewDragHandler>();
            SerializedObject dragSettings = new SerializedObject(dragHandler);
            dragSettings.FindProperty("previewRenderer").objectReferenceValue = previewRenderer;
            dragSettings.FindProperty("rotationSensitivity").floatValue = 0.35f;
            dragSettings.ApplyModifiedPropertiesWithoutUndo();

            Text hint = CreateText(
                "RotateHintText", previewPanel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0.08f),
                "마우스 왼쪽 버튼을 누른 채 드래그하여 회전",
                16, new Color(0.72f, 0.72f, 0.72f), defaultFont);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.raycastTarget = false;

            GameObject information = CreateRectObject(
                "ItemInfoPanel", overview.transform,
                new Vector2(0.60f, 0.075f), new Vector2(0.965f, 0.925f));

            Text title = CreateText(
                "ItemNameText", information.transform,
                new Vector2(0f, 0.82f), Vector2.one,
                "아이템 이름", 36, Color.white, defaultFont);
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.UpperLeft;

            Text description = CreateText(
                "DescriptionText", information.transform,
                new Vector2(0f, 0.25f), new Vector2(1f, 0.78f),
                "아이템 설명", 22, new Color(0.86f, 0.86f, 0.86f), defaultFont);
            description.alignment = TextAnchor.UpperLeft;

            Text inventoryHint = CreateText(
                "InventoryHintText", information.transform,
                new Vector2(0f, 0.205f), new Vector2(1f, 0.245f),
                "I 버튼을 눌러 인벤토리에서 확인할 수 있습니다.", 18,
                new Color(0.65f, 0.65f, 0.65f), defaultFont);
            inventoryHint.alignment = TextAnchor.MiddleLeft;
            inventoryHint.raycastTarget = false;

            Button readButton = CreateButton(
                "ReadButton", information.transform,
                new Vector2(0f, 0.11f), new Vector2(1f, 0.20f),
                "자세하게 읽기", defaultFont);
            Button overviewCloseButton = CreateButton(
                "CloseButton", information.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0.09f),
                "닫기", defaultFont);

            GameObject reader = CreateRectObject(
                "PaperReader", canvasObject.transform, Vector2.zero, Vector2.one);
            Image paperBackground = CreateImage(
                "PaperBackground", reader.transform,
                new Vector2(0.11f, 0.07f), new Vector2(0.89f, 0.93f),
                new Color(0.92f, 0.89f, 0.79f));

            Text paperTitle = CreateText(
                "PaperTitleText", paperBackground.transform,
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f),
                "문서 제목", 34, new Color(0.12f, 0.1f, 0.08f), defaultFont);
            paperTitle.fontStyle = FontStyle.Bold;
            paperTitle.alignment = TextAnchor.MiddleCenter;

            Text pageNumber = CreateText(
                "PageNumberText", paperBackground.transform,
                new Vector2(0.4f, 0.81f), new Vector2(0.6f, 0.86f),
                "1 / 2", 16, new Color(0.3f, 0.27f, 0.22f), defaultFont);
            pageNumber.alignment = TextAnchor.MiddleCenter;

            Image paperImage = CreateImage(
                "PaperImage", paperBackground.transform,
                new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.80f),
                Color.white);
            paperImage.preserveAspect = true;

            Text paperBody = CreateText(
                "PaperBodyText", paperBackground.transform,
                new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.78f),
                "종이 내용", 21, new Color(0.12f, 0.1f, 0.08f), defaultFont);
            paperBody.alignment = TextAnchor.UpperLeft;

            GameObject navigation = CreateRectObject(
                "Navigation", paperBackground.transform,
                new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.14f));
            Button previousButton = CreateButton(
                "PreviousButton", navigation.transform,
                new Vector2(0f, 0f), new Vector2(0.31f, 1f),
                "이전 페이지", defaultFont);
            Button nextButton = CreateButton(
                "NextButton", navigation.transform,
                new Vector2(0.345f, 0f), new Vector2(0.655f, 1f),
                "다음 페이지", defaultFont);
            Button readerCloseButton = CreateButton(
                "CloseButton", navigation.transform,
                new Vector2(0.69f, 0f), new Vector2(1f, 1f),
                "닫기", defaultFont);

            UnityEventTools.AddPersistentListener(
                readButton.onClick, controller.OpenPaperReader);
            UnityEventTools.AddPersistentListener(
                overviewCloseButton.onClick, controller.CloseInspection);
            UnityEventTools.AddPersistentListener(
                previousButton.onClick, controller.ShowPreviousPage);
            UnityEventTools.AddPersistentListener(
                nextButton.onClick, controller.ShowNextPage);
            UnityEventTools.AddPersistentListener(
                readerCloseButton.onClick, controller.ClosePaperReader);

            SerializedObject controllerSettings = new SerializedObject(controller);
            controllerSettings.FindProperty("canvasRoot").objectReferenceValue = canvasObject;
            controllerSettings.FindProperty("itemOverview").objectReferenceValue = overview;
            controllerSettings.FindProperty("paperReader").objectReferenceValue = reader;
            controllerSettings.FindProperty("previewImage").objectReferenceValue = previewImage;
            controllerSettings.FindProperty("itemTitle").objectReferenceValue = title;
            controllerSettings.FindProperty("itemDescription").objectReferenceValue = description;
            controllerSettings.FindProperty("readButton").objectReferenceValue = readButton;
            controllerSettings.FindProperty("paperTitle").objectReferenceValue = paperTitle;
            controllerSettings.FindProperty("pageNumber").objectReferenceValue = pageNumber;
            controllerSettings.FindProperty("paperImage").objectReferenceValue = paperImage;
            controllerSettings.FindProperty("paperText").objectReferenceValue = paperBody;
            controllerSettings.FindProperty("previousButton").objectReferenceValue = previousButton;
            controllerSettings.FindProperty("nextButton").objectReferenceValue = nextButton;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();

            reader.SetActive(false);
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
            label.text = text;
            label.font = font;
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
            string label, Font font)
        {
            Image background = CreateImage(
                name, parent, anchorMin, anchorMax,
                new Color(0.17f, 0.17f, 0.19f));
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            Text text = CreateText(
                "Text", background.transform,
                Vector2.zero, Vector2.one,
                label, 20, Color.white, font);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();
        }

        private static void ConfigureExistingKeys(GameObject fallbackPreview)
        {
            GameObject keyPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/Key.prefab");
            GameObject preview = keyPrefab != null ? keyPrefab : fallbackPreview;
            for (int index = 1; index <= 3; index++)
            {
                string keyId = $"Key_0{index}";
                ItemDefinition definition = CreateItemDefinition(
                    $"{DataDirectory}/{keyId}.asset",
                    keyId,
                    $"출구 열쇠 {index}",
                    ItemType.Key,
                    "공장 출구의 잠금장치를 해제하는 열쇠다. 출구를 열려면 총 세 개가 필요하다.",
                    preview,
                    1.25f);

                GameObject keyObject = GameObject.Find(keyId);
                if (keyObject == null)
                {
                    Debug.LogWarning($"{keyId} was not found in DoYoungPracticeScene.");
                    continue;
                }

                KeyCollectible collectible = keyObject.GetComponent<KeyCollectible>();
                if (collectible == null)
                {
                    continue;
                }

                SerializedObject keySettings = new SerializedObject(collectible);
                keySettings.FindProperty("itemDefinition").objectReferenceValue = definition;
                keySettings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject CreatePreviewPrefab(
            string fileName,
            PrimitiveType primitiveType,
            Vector3 scale,
            Color color)
        {
            string materialPath = $"{MaterialDirectory}/{fileName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { color = color };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            GameObject temporary = GameObject.CreatePrimitive(primitiveType);
            temporary.name = fileName;
            temporary.transform.localScale = scale;
            temporary.GetComponent<Renderer>().sharedMaterial = material;
            string prefabPath = $"{PrefabDirectory}/{fileName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, prefabPath);
            Object.DestroyImmediate(temporary);
            return prefab;
        }

        private static GameObject CreatePaperPreviewPrefab()
        {
            string materialPath = $"{MaterialDirectory}/FactoryMemo.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { color = new Color(0.76f, 0.72f, 0.58f) };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temporary.name = "FactoryMemo";
            temporary.transform.localScale = new Vector3(1.2f, 0.04f, 0.82f);
            temporary.GetComponent<Renderer>().sharedMaterial = material;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                temporary, $"{PrefabDirectory}/FactoryMemo.prefab");
            Object.DestroyImmediate(temporary);
            return prefab;
        }

        private static ItemDefinition CreateItemDefinition(
            string path,
            string itemId,
            string displayName,
            ItemType type,
            string description,
            GameObject previewPrefab,
            float previewScale)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject settings = new SerializedObject(item);
            settings.FindProperty("itemId").stringValue = itemId;
            settings.FindProperty("displayName").stringValue = displayName;
            settings.FindProperty("itemType").enumValueIndex = (int)type;
            settings.FindProperty("description").stringValue = description;
            settings.FindProperty("previewPrefab").objectReferenceValue = previewPrefab;
            settings.FindProperty("previewScale").floatValue = previewScale;
            settings.FindProperty("inspectOnPickup").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void SetPaperPages(ItemDefinition paper)
        {
            SerializedObject settings = new SerializedObject(paper);
            SerializedProperty pages = settings.FindProperty("pages");
            pages.arraySize = 2;
            pages.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue =
                "안무 검사 기록 27일차\n\n오늘도 3번 생산선에서 같은 결함이 발견되었다. " +
                "명령을 기다리지 않고 주위를 살피는 인형이 있다. 감독관은 단순 센서 오류라고 했지만, " +
                "그 인형은 내가 보고 있다는 사실을 아는 것처럼 고개를 돌렸다.";
            pages.GetArrayElementAtIndex(1).FindPropertyRelative("text").stringValue =
                "폐기 지시\n\n결함 개체는 다음 검사 종료 후 즉시 폐기 구역으로 이송한다. " +
                "만약 개체가 다른 인형의 안무를 완벽히 따라 할 경우 가까이 접근하지 말고 감시자를 호출할 것.\n\n" +
                "추신: 출구 열쇠는 세 명의 관리자에게 나누어 보관했다.";
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(paper);
        }

        private static void CreateWorldItem(
            string objectName,
            ItemDefinition definition,
            GameObject visualPrefab,
            Vector3 position,
            Transform parent)
        {
            GameObject itemObject = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
            itemObject.name = objectName;
            itemObject.transform.SetParent(parent);
            itemObject.transform.position = position;
            itemObject.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

            Collider itemCollider = itemObject.GetComponent<Collider>();
            itemCollider.isTrigger = true;
            InspectableItem inspectable = itemObject.AddComponent<InspectableItem>();
            SerializedObject settings = new SerializedObject(inspectable);
            settings.FindProperty("item").objectReferenceValue = definition;
            settings.ApplyModifiedPropertiesWithoutUndo();
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
