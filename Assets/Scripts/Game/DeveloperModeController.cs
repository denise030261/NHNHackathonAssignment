using NHNHackathon.Dance;
using NHNHackathon.Items;
using NHNHackathon.Progression;
using UnityEngine;

namespace NHNHackathon.Game
{
    [DisallowMultipleComponent]
    public sealed class DeveloperModeController : MonoBehaviour
    {
        [Header("Developer Mode")]
        [SerializeField] private bool developerModeEnabled;
        [SerializeField] private bool ignoreWatchers = true;

        [Header("Runtime Toggle")]
        [SerializeField] private bool allowRuntimeToggle = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool logStateChanges = true;

        public static DeveloperModeController Instance { get; private set; }
        public static bool IsEnabled => Instance != null && Instance.developerModeEnabled;
        public static bool ShouldWatchersIgnorePlayer =>
            IsEnabled && Instance.ignoreWatchers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LogCurrentState();
        }

        private void Update()
        {
            bool controlHeld = UnityEngine.Input.GetKey(KeyCode.LeftControl)
                || UnityEngine.Input.GetKey(KeyCode.RightControl);
            bool shiftHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift)
                || UnityEngine.Input.GetKey(KeyCode.RightShift);
            if (!allowRuntimeToggle || !controlHeld || !shiftHeld
                || !UnityEngine.Input.GetKeyDown(toggleKey))
            {
                return;
            }

            SetDeveloperMode(!developerModeEnabled);
        }

        private void Start()
        {
            if (developerModeEnabled)
            {
                GrantDeveloperProgress();
            }
        }

        public void SetDeveloperMode(bool enabled)
        {
            if (developerModeEnabled == enabled)
            {
                return;
            }

            developerModeEnabled = enabled;
            if (developerModeEnabled)
            {
                GrantDeveloperProgress();
            }
            LogCurrentState();
        }

        private static void GrantDeveloperProgress()
        {
            PlayerItemInventory inventory = FindFirstObjectByType<PlayerItemInventory>(
                FindObjectsInactive.Include);
            if (inventory != null)
            {
                DeveloperModeKeyCatalog keyCatalog =
                    Resources.Load<DeveloperModeKeyCatalog>("Data/DeveloperModeKeyCatalog");
                if (keyCatalog != null)
                {
                    foreach (ItemDefinition item in keyCatalog.Keys)
                    {
                        if (item != null && item.Type == ItemType.Key)
                        {
                            inventory.TryCollect(item);
                            GameProgressionController.Instance?.TryComplete(
                                item.ProgressionCondition);
                        }
                    }

                    inventory.TryCollect(keyCatalog.Flashlight);
                }

                foreach (KeyCollectible collectible in FindObjectsByType<KeyCollectible>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (collectible.ItemDefinition != null
                        && inventory.Contains(collectible.ItemDefinition))
                    {
                        collectible.ApplyCollectedState();
                    }
                }
            }

            PlayerDanceUnlockController danceUnlockController =
                FindFirstObjectByType<PlayerDanceUnlockController>(FindObjectsInactive.Include);
            danceUnlockController?.UnlockAll();
        }

        private void LogCurrentState()
        {
            if (!logStateChanges)
            {
                return;
            }

            Debug.Log($"Developer Mode: {(developerModeEnabled ? "ON" : "OFF")}", this);
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
