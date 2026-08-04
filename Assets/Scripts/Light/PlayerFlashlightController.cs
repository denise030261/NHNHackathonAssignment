using NHNHackathon.Interaction;
using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.LightSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LightStimulusSource))]
    public sealed class PlayerFlashlightController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F;
        [SerializeField] private Light flashlight;
        [SerializeField] private bool startEnabled;

        [Header("Inventory Requirement")]
        [SerializeField] private PlayerItemInventory playerInventory;
        [SerializeField, Tooltip("The flashlight can only be toggled while this item is owned.")]
        private ItemDefinition requiredFlashlightItem;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private string missingItemMessage = "손전등이 필요합니다.";
        [SerializeField, Min(0f)] private float missingItemMessageDuration = 1.5f;

        public bool CanUseFlashlight => requiredFlashlightItem == null
            || playerInventory != null && playerInventory.Contains(requiredFlashlightItem);

        private void Awake()
        {
            playerInventory ??= GetComponentInParent<PlayerItemInventory>();
            playerInteractor ??= GetComponentInParent<PlayerInteractor>();
        }

        private void OnEnable()
        {
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void Start()
        {
            SetFlashlight(startEnabled && CanUseFlashlight);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                if (!CanUseFlashlight)
                {
                    SetFlashlight(false);
                    playerInteractor?.ShowTemporaryMessage(
                        missingItemMessage, missingItemMessageDuration);
                    return;
                }
                SetFlashlight(!flashlight.enabled);
            }
        }

        private void OnDisable()
        {
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandleInventoryChanged()
        {
            if (!CanUseFlashlight)
            {
                SetFlashlight(false);
            }
        }

        private void SetFlashlight(bool value)
        {
            if (flashlight != null)
            {
                flashlight.enabled = value;
            }
        }

        private void OnValidate()
        {
            if (flashlight == null)
            {
                flashlight = GetComponent<Light>();
            }
            playerInventory ??= GetComponentInParent<PlayerItemInventory>();
            playerInteractor ??= GetComponentInParent<PlayerInteractor>();

            if (requiredFlashlightItem != null
                && requiredFlashlightItem.Type != ItemType.General)
            {
                Debug.LogWarning(
                    $"{name}: Required Flashlight Item should use the General item type.", this);
            }
        }
    }
}
