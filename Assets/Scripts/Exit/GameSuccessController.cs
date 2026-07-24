using System;
using NHNHackathon.Enemy;
using NHNHackathon.Game;
using UnityEngine;
using UnityEngine.AI;

namespace NHNHackathon.ExitSystem
{
    [DisallowMultipleComponent]
    public sealed class GameSuccessController : MonoBehaviour
    {
        [SerializeField] private GameOverController gameOverController;
        [SerializeField] private Behaviour[] playerControls;
        [SerializeField] private string successText = "ESCAPED";
        [SerializeField, Min(12)] private int fontSize = 72;

        public event Action GameSucceeded;

        public bool IsSuccessful { get; private set; }

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

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameSucceeded?.Invoke();
        }

        private void OnGUI()
        {
            if (!IsSuccessful)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.7f, 1f, 0.75f);
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), successText, style);
            GUI.color = previousColor;
        }
    }
}
