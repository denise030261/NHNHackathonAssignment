using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Items
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "NHN Hackathon/Items/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "Item_01";
        [SerializeField] private string displayName = "Item";
        [SerializeField] private ItemType itemType;

        [Header("Inspection")]
        [SerializeField, TextArea(2, 6)] private string description;
        [SerializeField, Tooltip("Prefab instantiated by the isolated preview camera.")]
        private GameObject previewPrefab;
        [SerializeField] private Vector3 previewEulerAngles;
        [SerializeField, Min(0.01f)] private float previewScale = 1f;
        [SerializeField] private bool inspectOnPickup = true;

        [Header("Paper")]
        [SerializeField, Tooltip("Paper supports one or two pages.")]
        private List<PaperPageDefinition> pages = new List<PaperPageDefinition>();

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ItemType Type => itemType;
        public string Description => description;
        public GameObject PreviewPrefab => previewPrefab;
        public Vector3 PreviewEulerAngles => previewEulerAngles;
        public float PreviewScale => previewScale;
        public bool InspectOnPickup => inspectOnPickup;
        public IReadOnlyList<PaperPageDefinition> Pages => pages;
        public bool CanRead => itemType == ItemType.Paper && pages.Count > 0;

        private void OnValidate()
        {
            previewScale = Mathf.Max(0.01f, previewScale);
            if (pages.Count > 2)
            {
                pages.RemoveRange(2, pages.Count - 2);
            }
        }
    }
}
