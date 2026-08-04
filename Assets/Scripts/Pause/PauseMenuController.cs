using System.Collections.Generic;
using NHNHackathon.ExitSystem;
using NHNHackathon.Game;
using NHNHackathon.Inspection;
using NHNHackathon.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.Pause
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        [Header("Screens")]
        [SerializeField] private GameObject pauseRoot;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Scene")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Modal Conflicts")]
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private ItemInspectionController inspectionController;
        [SerializeField] private GameOverController gameOverController;
        [SerializeField] private GameSuccessController gameSuccessController;

        [Header("Gameplay Controls")]
        [SerializeField] private Behaviour[] controlledBehaviours;

        private readonly List<bool> previousEnabledStates = new();
        private float previousTimeScale = 1f;
        private bool isPaused;
        private bool isLoadingMainMenu;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            SetScreenState(false, true, false);
        }

        private void Update()
        {
            if (isLoadingMainMenu || !UnityEngine.Input.GetKeyDown(pauseKey))
            {
                return;
            }

            if (isPaused)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else
                {
                    ResumeGame();
                }
                return;
            }

            if (CanOpenPauseMenu())
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (isPaused || !CanOpenPauseMenu())
            {
                return;
            }

            isPaused = true;
            previousTimeScale = Time.timeScale;
            previousEnabledStates.Clear();
            foreach (Behaviour behaviour in controlledBehaviours)
            {
                previousEnabledStates.Add(behaviour != null && behaviour.enabled);
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetScreenState(true, true, false);
        }

        public void ResumeGame()
        {
            if (!isPaused)
            {
                return;
            }

            RestoreGameplayState();
            SetScreenState(false, true, false);
        }

        public void OpenSettings()
        {
            if (isPaused)
            {
                SetScreenState(true, false, true);
            }
        }

        public void CloseSettings()
        {
            if (isPaused)
            {
                SetScreenState(true, true, false);
            }
        }

        public void ReturnToMainMenu()
        {
            if (isLoadingMainMenu)
            {
                return;
            }

            isLoadingMainMenu = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadSceneAsync(mainMenuSceneName);
        }

        private bool CanOpenPauseMenu()
        {
            bool inventoryOpen = inventoryController != null && inventoryController.IsOpen;
            bool inspectionOpen = inspectionController != null
                && inspectionController.State != InspectionViewState.Closed;
            bool gameEnded = gameOverController != null && gameOverController.IsGameOver
                || gameSuccessController != null && gameSuccessController.IsSuccessful;
            return !inventoryOpen && !inspectionOpen && !gameEnded;
        }

        private void RestoreGameplayState()
        {
            Time.timeScale = previousTimeScale;
            int restoreCount = Mathf.Min(
                controlledBehaviours.Length, previousEnabledStates.Count);
            for (int index = 0; index < restoreCount; index++)
            {
                Behaviour behaviour = controlledBehaviours[index];
                if (behaviour != null)
                {
                    behaviour.enabled = previousEnabledStates[index];
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isPaused = false;
        }

        private void SetScreenState(
            bool rootVisible, bool pauseVisible, bool settingsVisible)
        {
            if (pauseRoot != null)
            {
                pauseRoot.SetActive(rootVisible);
            }
            if (pausePanel != null)
            {
                pausePanel.SetActive(pauseVisible);
            }
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(settingsVisible);
            }
        }

        private void OnDestroy()
        {
            if (isPaused && !isLoadingMainMenu)
            {
                RestoreGameplayState();
            }
        }
    }
}
