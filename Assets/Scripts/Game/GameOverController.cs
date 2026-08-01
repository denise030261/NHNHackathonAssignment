using System;
using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Game
{
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private Behaviour[] playerControls;

        [Header("Game Over UI")]
        [Tooltip("Root GameObject of the Game Over UI placed in the Hierarchy.")]
        [SerializeField] private GameObject gameOverUI;
        [Tooltip("Optional Text component used to display the Game Over message.")]
        [SerializeField] private Text gameOverText;

        public event Action GameOverTriggered;

        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            SetGameOverUIVisible(false);
        }

        public void TriggerGameOver()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            foreach (Behaviour control in playerControls)
            {
                if (control != null)
                {
                    control.enabled = false;
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            SetGameOverUIVisible(true);
            GameOverTriggered?.Invoke();
        }

        public void SetGameOverMessage(string message)
        {
            if (gameOverText != null)
            {
                gameOverText.text = message;
            }
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
