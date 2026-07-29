using NHNHackathon.Items;
using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Inspection
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ItemPreviewRenderer), typeof(InspectionControlLock))]
    public sealed class ItemInspectionController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Screen Roots")]
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject itemOverview;
        [SerializeField] private GameObject paperReader;

        [Header("Item Overview")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private Text itemTitle;
        [SerializeField] private Text itemDescription;
        [SerializeField] private Button readButton;

        [Header("Paper Reader")]
        [SerializeField] private Text paperTitle;
        [SerializeField] private Text pageNumber;
        [SerializeField] private Image paperImage;
        [SerializeField] private Text paperText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;

        private ItemPreviewRenderer previewRenderer;
        private InspectionControlLock controlLock;
        private ItemDefinition currentItem;
        private InspectionViewState state;
        private int currentPageIndex;

        public static ItemInspectionController Instance { get; private set; }
        public InspectionViewState State => state;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            previewRenderer = GetComponent<ItemPreviewRenderer>();
            controlLock = GetComponent<InspectionControlLock>();
            SetScreenState(false, false, false);
        }

        private void Update()
        {
            if (state != InspectionViewState.Closed
                && UnityEngine.Input.GetKeyDown(closeKey))
            {
                if (state == InspectionViewState.PaperReader)
                {
                    ClosePaperReader();
                }
                else
                {
                    CloseInspection();
                }
            }
        }

        public void Open(ItemDefinition item)
        {
            if (item == null || state != InspectionViewState.Closed)
            {
                return;
            }

            currentItem = item;
            currentPageIndex = 0;
            state = InspectionViewState.ItemOverview;

            previewRenderer.Show(item);
            previewImage.texture = previewRenderer.Texture;
            itemTitle.text = item.DisplayName;
            itemDescription.text = item.Description;
            readButton.gameObject.SetActive(item.CanRead);
            SetScreenState(true, true, false);
            controlLock.Lock();
        }

        public void OpenPaperReader()
        {
            if (currentItem == null || !currentItem.CanRead)
            {
                return;
            }

            state = InspectionViewState.PaperReader;
            currentPageIndex = 0;
            RefreshPaperPage();
            SetScreenState(true, false, true);
        }

        public void ClosePaperReader()
        {
            if (currentItem == null)
            {
                return;
            }

            state = InspectionViewState.ItemOverview;
            SetScreenState(true, true, false);
        }

        public void ShowPreviousPage()
        {
            ChangePage(-1);
        }

        public void ShowNextPage()
        {
            ChangePage(1);
        }

        public void CloseInspection()
        {
            if (state == InspectionViewState.Closed)
            {
                return;
            }

            previewRenderer.Clear();
            previewImage.texture = null;
            currentItem = null;
            state = InspectionViewState.Closed;
            SetScreenState(false, false, false);
            controlLock.Unlock();
        }

        private void ChangePage(int offset)
        {
            if (currentItem == null || !currentItem.CanRead)
            {
                return;
            }

            currentPageIndex = Mathf.Clamp(
                currentPageIndex + offset, 0, currentItem.Pages.Count - 1);
            RefreshPaperPage();
        }

        private void RefreshPaperPage()
        {
            PaperPageDefinition page = currentItem.Pages[currentPageIndex];
            paperTitle.text = currentItem.DisplayName;
            pageNumber.text = $"{currentPageIndex + 1} / {currentItem.Pages.Count}";
            paperText.text = page.Text;
            paperImage.sprite = page.Image;
            paperImage.gameObject.SetActive(page.Image != null);
            previousButton.interactable = currentPageIndex > 0;
            nextButton.interactable = currentPageIndex < currentItem.Pages.Count - 1;
        }

        private void SetScreenState(bool canvasVisible, bool overviewVisible, bool readerVisible)
        {
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(canvasVisible);
            }
            if (itemOverview != null)
            {
                itemOverview.SetActive(overviewVisible);
            }
            if (paperReader != null)
            {
                paperReader.SetActive(readerVisible);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
