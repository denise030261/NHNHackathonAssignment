#if UNITY_EDITOR
using System.Linq;
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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.EditorTools
{
    public static class Level1PlayerUISetup
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NHN Hackathon/UI/Connect Level1 UI To Player")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindSceneObject("Player");
            GameObject uiRoot = FindSceneObject("UI");
            if (player == null || uiRoot == null)
            {
                throw new System.InvalidOperationException("Level1 requires Player and UI objects.");
            }

            PlayerItemInventory itemInventory = GetOrAdd<PlayerItemInventory>(player);
            PlayerInteractor interactor = GetOrAdd<PlayerInteractor>(player);
            RemovePlayerHostedUISystems(player);
            DeleteSceneObject("InventorySystem");
            DeleteSceneObject("ItemInspectionSystem");

            GameObject inspectionSystem = new GameObject("ItemInspectionSystem");
            ItemPreviewRenderer inspectionPreview = inspectionSystem.AddComponent<ItemPreviewRenderer>();
            InspectionControlLock inspectionLock = inspectionSystem.AddComponent<InspectionControlLock>();
            ItemInspectionController inspection = inspectionSystem.AddComponent<ItemInspectionController>();
            ConfigurePreviewCamera(
                inspectionSystem.transform, "ItemPreviewCamera", inspectionPreview);

            GameObject inventorySystem = new GameObject("InventorySystem");
            ItemPreviewRenderer inventoryPreview = inventorySystem.AddComponent<ItemPreviewRenderer>();
            InspectionControlLock inventoryLock = inventorySystem.AddComponent<InspectionControlLock>();
            InventoryController inventory = inventorySystem.AddComponent<InventoryController>();
            ConfigurePreviewCamera(
                inventorySystem.transform, "InventoryPreviewCamera", inventoryPreview);

            ConfigureHud(uiRoot.transform, interactor);
            ConfigureInspection(uiRoot.transform, inspection, inspectionPreview);
            ConfigureInventory(
                uiRoot.transform, inventory, inspection, itemInventory, inventoryPreview);
            ConfigureControlLock(player, inspectionLock, interactor);
            ConfigureControlLock(player, inventoryLock, interactor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Level1 UI connected to the Player inspector.");
        }

        private static void ConfigureHud(Transform uiRoot, PlayerInteractor interactor)
        {
            GameObject prompt = RequireDescendant(uiRoot, "InteractionPrompt").gameObject;
            GameObject counter = RequireDescendant(uiRoot, "KeyCounter").gameObject;
            GameObject notification = RequireDescendant(uiRoot, "KeyNotification").gameObject;

            SerializedObject interactorSettings = new SerializedObject(interactor);
            interactorSettings.FindProperty("promptRoot").objectReferenceValue = prompt;
            interactorSettings.FindProperty("promptText").objectReferenceValue = prompt.GetComponent<Text>();
            interactorSettings.ApplyModifiedPropertiesWithoutUndo();

            prompt.SetActive(false);
            counter.SetActive(false);
            notification.SetActive(false);
        }

        private static void ConfigureInventory(
            Transform uiRoot, InventoryController controller,
            ItemInspectionController inspection, PlayerItemInventory itemInventory,
            ItemPreviewRenderer previewRenderer)
        {
            Transform root = RequireDescendant(uiRoot, "InventoryUI");
            NormalizePanel(root as RectTransform);
            Transform content = RequireDescendant(root, "Content");
            Button template = RequireDescendant(root, "ItemSlotTemplate").GetComponent<Button>();
            GameObject detailPanel = RequireDescendant(root, "ItemDetailPanel").gameObject;
            Transform previewPanel = RequireDescendant(root, "ItemPreviewPanel");
            RawImage previewImage = previewPanel.GetComponent<RawImage>();
            Text itemName = RequireDescendant(detailPanel.transform, "ItemNameText").GetComponent<Text>();
            Text description = RequireDescendant(detailPanel.transform, "ItemDescriptionText").GetComponent<Text>();
            Button readButton = RequireDescendant(detailPanel.transform, "ReadButton").GetComponent<Button>();
            Button closeButton = RequireDescendant(root, "Header")
                .GetComponentsInChildren<Button>(true).First(value => value.name == "CloseButton");

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("inventory").objectReferenceValue = itemInventory;
            settings.FindProperty("inspectionController").objectReferenceValue = inspection;
            settings.FindProperty("canvasRoot").objectReferenceValue = root.gameObject;
            settings.FindProperty("itemListContent").objectReferenceValue = content;
            settings.FindProperty("itemButtonTemplate").objectReferenceValue = template;
            settings.FindProperty("detailPanel").objectReferenceValue = detailPanel;
            settings.FindProperty("previewImage").objectReferenceValue = previewImage;
            settings.FindProperty("itemNameText").objectReferenceValue = itemName;
            settings.FindProperty("itemDescriptionText").objectReferenceValue = description;
            settings.FindProperty("readButton").objectReferenceValue = readButton;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Bind(closeButton, controller.CloseInventory);
            Bind(readButton, controller.ReadSelectedPaper);
            ConfigureDragHandler(previewPanel, previewRenderer);
            template.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        private static void ConfigureInspection(
            Transform uiRoot, ItemInspectionController controller,
            ItemPreviewRenderer previewRenderer)
        {
            Transform root = RequireDescendant(uiRoot, "ItemInspectionUI");
            NormalizePanel(root as RectTransform);
            GameObject overview = RequireDescendant(root, "ItemOverview").gameObject;
            GameObject paperReader = RequireDescendant(root, "PaperReadingUI").gameObject;
            Transform previewPanel = RequireDescendant(overview.transform, "PreviewPanel");
            RawImage previewImage = previewPanel.GetComponent<RawImage>();
            Transform info = RequireDescendant(overview.transform, "ItemInfoPanel");
            Text title = RequireDescendant(info, "ItemNameText").GetComponent<Text>();
            Text description = RequireDescendant(info, "DescriptionText").GetComponent<Text>();
            EnsureInventoryHint(info, description.font);
            Button readButton = RequireDescendant(info, "ReadButton").GetComponent<Button>();
            Button overviewClose = RequireDescendant(info, "CloseButton").GetComponent<Button>();

            Text paperTitle = RequireDescendant(paperReader.transform, "PaperTitleText").GetComponent<Text>();
            Text pageNumber = RequireDescendant(paperReader.transform, "PageNumberText").GetComponent<Text>();
            Image paperImage = RequireDescendant(paperReader.transform, "PaperImage").GetComponent<Image>();
            Text paperBody = RequireDescendant(paperReader.transform, "PaperBodyText").GetComponent<Text>();
            Button previous = RequireDescendant(paperReader.transform, "PreviousButton").GetComponent<Button>();
            Button next = RequireDescendant(paperReader.transform, "NextButton").GetComponent<Button>();
            Transform navigation = RequireDescendant(paperReader.transform, "Navigation");
            Button readerClose = navigation.GetComponentsInChildren<Button>(true)
                .First(value => value.name == "CloseButton");

            SerializedObject settings = new SerializedObject(controller);
            settings.FindProperty("canvasRoot").objectReferenceValue = root.gameObject;
            settings.FindProperty("itemOverview").objectReferenceValue = overview;
            settings.FindProperty("paperReader").objectReferenceValue = paperReader;
            settings.FindProperty("previewImage").objectReferenceValue = previewImage;
            settings.FindProperty("itemTitle").objectReferenceValue = title;
            settings.FindProperty("itemDescription").objectReferenceValue = description;
            settings.FindProperty("readButton").objectReferenceValue = readButton;
            settings.FindProperty("paperTitle").objectReferenceValue = paperTitle;
            settings.FindProperty("pageNumber").objectReferenceValue = pageNumber;
            settings.FindProperty("paperImage").objectReferenceValue = paperImage;
            settings.FindProperty("paperText").objectReferenceValue = paperBody;
            settings.FindProperty("previousButton").objectReferenceValue = previous;
            settings.FindProperty("nextButton").objectReferenceValue = next;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Bind(readButton, controller.OpenPaperReader);
            Bind(overviewClose, controller.CloseInspection);
            Bind(previous, controller.ShowPreviousPage);
            Bind(next, controller.ShowNextPage);
            Bind(readerClose, controller.ClosePaperReader);
            ConfigureDragHandler(previewPanel, previewRenderer);
            paperReader.SetActive(false);
            root.gameObject.SetActive(false);
        }

        private static Camera ConfigurePreviewCamera(
            Transform parent, string cameraName, ItemPreviewRenderer previewRenderer)
        {
            Transform existing = parent.Find(cameraName);
            GameObject cameraObject = existing != null
                ? existing.gameObject
                : new GameObject(cameraName);
            cameraObject.transform.SetParent(parent, false);
            Camera previewCamera = GetOrAdd<Camera>(cameraObject);
            previewCamera.enabled = true;
            previewCamera.fieldOfView = 35f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 20f;
            Light previewLight = GetOrAdd<Light>(cameraObject);
            previewLight.type = LightType.Directional;
            previewLight.cullingMask = 1 << 31;

            SerializedObject settings = new SerializedObject(previewRenderer);
            settings.FindProperty("previewCamera").objectReferenceValue = previewCamera;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return previewCamera;
        }

        private static void RemovePlayerHostedUISystems(GameObject player)
        {
            RemoveIfPresent<InventoryController>(player);
            RemoveIfPresent<ItemInspectionController>(player);
            RemoveIfPresent<ItemPreviewRenderer>(player);
            RemoveIfPresent<InspectionControlLock>(player);
            Transform previewCamera = player.transform.Find("UIItemPreviewCamera");
            if (previewCamera != null)
            {
                Object.DestroyImmediate(previewCamera.gameObject);
            }
        }

        private static void DeleteSceneObject(string name)
        {
            GameObject target = FindSceneObject(name);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void RemoveIfPresent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }

        private static void ConfigureControlLock(
            GameObject player, InspectionControlLock controlLock, PlayerInteractor interactor)
        {
            Behaviour[] controls =
            {
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<PlayerCameraController>(),
                player.GetComponent<PlayerDanceInput>(),
                player.GetComponent<PlayerCursorController>(),
                player.GetComponentInChildren<PlayerFlashlightController>(true),
                interactor
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

        private static void ConfigureDragHandler(
            Transform target, ItemPreviewRenderer previewRenderer)
        {
            ItemPreviewDragHandler handler = GetOrAdd<ItemPreviewDragHandler>(target.gameObject);
            SerializedObject settings = new SerializedObject(handler);
            settings.FindProperty("previewRenderer").objectReferenceValue = previewRenderer;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static Transform RequireDescendant(Transform parent, string name)
        {
            Transform result = parent.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == name);
            return result != null
                ? result
                : throw new System.InvalidOperationException(
                    $"{name} was not found below {parent.name} in Level1.");
        }

        private static GameObject FindSceneObject(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(value => value.scene.IsValid() && value.name == name);
        }

        private static void NormalizePanel(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureInventoryHint(Transform parent, Font font)
        {
            Transform existing = parent.Find("InventoryHintText");
            GameObject hintObject = existing != null
                ? existing.gameObject
                : new GameObject("InventoryHintText", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
            hintObject.transform.SetParent(parent, false);

            RectTransform rect = hintObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.205f);
            rect.anchorMax = new Vector2(1f, 0.245f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text hint = hintObject.GetComponent<Text>();
            hint.text = "I 버튼을 눌러 인벤토리에서 확인할 수 있습니다.";
            hint.font = font;
            hint.fontSize = 18;
            hint.color = new Color(0.65f, 0.65f, 0.65f);
            hint.alignment = TextAnchor.MiddleLeft;
            hint.raycastTarget = false;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
#endif
