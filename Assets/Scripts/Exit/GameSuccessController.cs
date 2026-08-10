using System;
using NHNHackathon.AudioSystem;
using NHNHackathon.Enemy;
using NHNHackathon.Game;
using NHNHackathon.Interaction;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class GameSuccessController : MonoBehaviour
    {
        [SerializeField] private GameOverController gameOverController;
        [SerializeField] private Behaviour[] playerControls;

        [Header("Success UI")]
        [SerializeField] private GameObject gameSuccessUI;
        [SerializeField] private Text successText;

        [Header("Ending Credits")]
        [SerializeField] private EndingCreditsController endingCreditsController;

        public event Action GameSucceeded;

        public bool IsSuccessful { get; private set; }

        private void Awake()
        {
            SetSuccessUIVisible(false);
        }

        public void TriggerSuccess()
        {
            if (IsSuccessful || (gameOverController != null && gameOverController.IsGameOver))
            {
                return;
            }

            IsSuccessful = true;
            foreach (Behaviour control in playerControls)
            {
                if (control != null)
                {
                    control.enabled = false;
                }
            }

            foreach (EnemyController enemy in FindObjectsByType<EnemyController>())
            {
                if (enemy.TryGetComponent(out NavMeshAgent agent) && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                enemy.enabled = false;
            }

            SceneBgmPlayer.StopAll();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            Time.timeScale = 0f;
            SetSuccessUIVisible(false);
            if (endingCreditsController != null)
            {
                endingCreditsController.PlayEnding();
            }
            else
            {
                Debug.LogError("Ending Credits Controller is not assigned.", this);
            }
            GameSucceeded?.Invoke();
        }

        public void SetSuccessMessage(string message)
        {
            if (successText != null)
            {
                successText.text = message;
            }
        }

        private void SetSuccessUIVisible(bool isVisible)
        {
            if (gameSuccessUI != null)
            {
                gameSuccessUI.SetActive(isVisible);
            }
        }
    }
}
