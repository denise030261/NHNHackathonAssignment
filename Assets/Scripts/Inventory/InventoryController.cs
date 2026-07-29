using System.Collections.Generic;
using NHNHackathon.Inspection;
using NHNHackathon.Items;
using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ItemPreviewRenderer), typeof(InspectionControlLock))]
    public sealed class InventoryController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Data")]
        [SerializeField] private PlayerItemInventory inventory;
        [SerializeField] private ItemInspectionController inspectionController;

        [Header("Screen")]
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private Transform itemListContent;
        [SerializeField] private Button itemButtonTemplate;
        [SerializeField] private GameObject detailPanel;

        [Header("Item Detail")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Button readButton;

        private readonly List<InventorySlot> spawnedSlots = new List<InventorySlot>();
        private ItemPreviewRenderer previewRenderer;
        private InspectionControlLock controlLock;
        private ItemDefinition selectedItem;
        private bool isOpen;
        private bool isReadingPaper;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            previewRenderer = GetComponent<ItemPreviewRenderer>();
            controlLock = GetComponent<InspectionControlLock>();
            canvasRoot.SetActive(false);
            itemButtonTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += RefreshItemList;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= RefreshItemList;
            }

            if (isOpen)
            {
                CloseInventory();
            }
        }

        private void Update()
        {
            if (isReadingPaper)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(inventoryKey))
            {
                if (isOpen)
                {
                    CloseInventory();
                }
                else
                {
                    OpenInventory();
                }
            }
            else if (isOpen && UnityEngine.Input.GetKeyDown(closeKey))
            {
                CloseInventory();
            }
        }

        public void OpenInventory()
        {
            if (isOpen || inventory == null)
            {
                return;
            }

            isOpen = true;
            canvasRoot.SetActive(true);
            controlLock.Lock();
            RefreshItemList();

            if (inventory.Items.Count > 0)
            {
                SelectItem(inventory.Items[0]);
            }
            else
            {
                ClearSelection();
            }
        }

        public void CloseInventory()
        {
            if (!isOpen || isReadingPaper)
            {
                return;
            }

            isOpen = false;
            canvasRoot.SetActive(false);
            ClearSelection();
            controlLock.Unlock();
        }

        public void ReadSelectedPaper()
        {
            if (!isOpen
                || selectedItem == null
                || !selectedItem.CanRead
                || inspectionController == null)
            {
                return;
            }

            isReadingPaper = true;
            canvasRoot.SetActive(false);
            previewRenderer.Clear();
            inspectionController.OpenPaperReaderFromInventory(
                selectedItem, ReturnFromPaperReader);
        }

        private void ReturnFromPaperReader()
        {
            isReadingPaper = false;
            canvasRoot.SetActive(true);
            ShowSelectedItem();
        }

        private void RefreshItemList()
        {
            if (inventory == null || itemButtonTemplate == null)
            {
                return;
            }

            foreach (InventorySlot slot in spawnedSlots)
            {
                if (slot.Button != null)
                {
                    Destroy(slot.Button.gameObject);
                }
            }
            spawnedSlots.Clear();

            foreach (ItemDefinition item in inventory.Items)
            {
                ItemDefinition capturedItem = item;
                Button button = Instantiate(itemButtonTemplate, itemListContent);
                button.name = $"ItemButton_{item.ItemId}";
                button.gameObject.SetActive(true);
                Image icon = FindChildImage(button.transform, "ItemIcon");
                if (icon != null)
                {
                    icon.sprite = item.InventoryIcon;
                    icon.enabled = item.InventoryIcon != null;
                }
                Outline selectionOutline = button.GetComponent<Outline>();
                if (selectionOutline != null)
                {
                    selectionOutline.enabled = false;
                }
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectItem(capturedItem));
                spawnedSlots.Add(new InventorySlot(capturedItem, button, selectionOutline));
            }
        }

        private void SelectItem(ItemDefinition item)
        {
            selectedItem = item;
            foreach (InventorySlot slot in spawnedSlots)
            {
                if (slot.SelectionOutline != null)
                {
                    slot.SelectionOutline.enabled = slot.Item == selectedItem;
                }
            }
            ShowSelectedItem();
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == childName)
                {
                    return image;
                }
            }

            return null;
        }

        private void ShowSelectedItem()
        {
            if (selectedItem == null)
            {
                ClearSelection();
                return;
            }

            detailPanel.SetActive(true);
            itemNameText.text = selectedItem.DisplayName;
            itemDescriptionText.text = selectedItem.Description;
            readButton.gameObject.SetActive(selectedItem.CanRead);
            previewRenderer.Show(selectedItem);
            previewImage.texture = previewRenderer.Texture;
        }

        private void ClearSelection()
        {
            selectedItem = null;
            previewRenderer.Clear();
            previewImage.texture = null;
            itemNameText.text = string.Empty;
            itemDescriptionText.text = string.Empty;
            readButton.gameObject.SetActive(false);
            detailPanel.SetActive(false);
        }

        private sealed class InventorySlot
        {
            public InventorySlot(
                ItemDefinition item, Button button, Outline selectionOutline)
            {
                Item = item;
                Button = button;
                SelectionOutline = selectionOutline;
            }

            public ItemDefinition Item { get; }
            public Button Button { get; }
            public Outline SelectionOutline { get; }
        }
    }
}
