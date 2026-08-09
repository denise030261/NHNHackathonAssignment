using System;
using NHNHackathon.AudioSystem;
using NHNHackathon.Characters;
using NHNHackathon.Enemy;
using NHNHackathon.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NHNHackathon.Game
{
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        [Header("Capture Sequence")]
        [SerializeField] private EnemyCaptureDirector captureDirector;
        [SerializeField] private Behaviour[] playerControls;

        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverUI;
        [SerializeField] private GameObject gameOverContent;
        [SerializeField] private Text gameOverText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isLoading;

        public event Action GameOverTriggered;
        public bool IsCapturePlaying { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            Time.timeScale = 1f;
            SetGameOverUIVisible(false);
        }

        public void TriggerGameOver()
        {
            TriggerGameOver(null);
        }

        public void TriggerGameOver(EnemyController attacker)
        {
            if (IsCapturePlaying || IsGameOver)
            {
                return;
            }

            IsCapturePlaying = true;
            SceneBgmPlayer.StopAll();
            PlayerCameraController cameraController =
                FindAnyObjectByType<PlayerCameraController>(FindObjectsInactive.Include);
            cameraController?.RequestPerspective(CameraPerspective.FirstPerson, 0f);
            SetPlayerControlsEnabled(false);
            MovePlayerToCapturePoint(attacker);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            SetGameOverUIVisible(true);
            if (gameOverContent != null)
            {
                gameOverContent.SetActive(false);
            }

            if (captureDirector != null && attacker != null)
            {
                captureDirector.Play(attacker, CompleteGameOver);
            }
            else
            {
                CompleteGameOver();
            }
        }

        public void RestartGame()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            PlayerCheckpointAgent checkpointAgent =
                FindAnyObjectByType<PlayerCheckpointAgent>(FindObjectsInactive.Include);
            checkpointAgent?.PrepareRespawn();
            PrepareSceneLoad();
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            PrepareSceneLoad();
            SceneManager.LoadSceneAsync(mainMenuSceneName);
        }

        public void SetGameOverMessage(string message)
        {
            if (gameOverText != null)
            {
                gameOverText.text = message;
            }
        }

        private void CompleteGameOver()
        {
            IsCapturePlaying = false;
            IsGameOver = true;
            if (gameOverContent != null)
            {
                gameOverContent.SetActive(true);
            }
            SetButtonsInteractable(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            GameOverTriggered?.Invoke();
        }

        private static void MovePlayerToCapturePoint(EnemyController attacker)
        {
            if (attacker == null)
            {
                return;
            }

            WatcherCapturePresenter presenter =
                attacker.GetComponentInChildren<WatcherCapturePresenter>(true);
            PlayerMovement movement =
                FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            if (movement == null)
            {
                return;
            }

            Transform player = movement.transform;
            presenter?.FaceTarget(player);

            Transform capturePoint = presenter != null
                ? presenter.PlayerCapturePoint
                : null;
            capturePoint ??= attacker.transform.Find("PlayerCapturePoint");
            if (capturePoint == null)
            {
                return;
            }

            CharacterController characterController =
                player.GetComponent<CharacterController>();
            bool controllerWasEnabled =
                characterController != null && characterController.enabled;
            if (controllerWasEnabled)
            {
                characterController.enabled = false;
            }

            player.SetPositionAndRotation(
                capturePoint.position, capturePoint.rotation);
            Physics.SyncTransforms();

            if (controllerWasEnabled)
            {
                characterController.enabled = true;
            }
        }

        private void PrepareSceneLoad()
        {
            SetButtonsInteractable(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetPlayerControlsEnabled(bool enabled)
        {
            foreach (Behaviour control in playerControls)
            {
                if (control != null)
                {
                    control.enabled = enabled;
                }
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (restartButton != null) restartButton.interactable = interactable;
            if (mainMenuButton != null) mainMenuButton.interactable = interactable;
        }

        private void SetGameOverUIVisible(bool isVisible)
        {
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(isVisible);
            }
        }
    }
}
