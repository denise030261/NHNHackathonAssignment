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
            if (!allowRuntimeToggle || !UnityEngine.Input.GetKeyDown(toggleKey))
            {
                return;
            }

            SetDeveloperMode(!developerModeEnabled);
        }

        public void SetDeveloperMode(bool enabled)
        {
            if (developerModeEnabled == enabled)
            {
                return;
            }

            developerModeEnabled = enabled;
            LogCurrentState();
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
