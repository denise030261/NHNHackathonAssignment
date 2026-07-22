using System;
using UnityEngine;

namespace NHNHackathon.Game
{
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private Behaviour[] playerControls;
        [SerializeField] private string gameOverText = "GAME OVER";
        [SerializeField, Min(12)] private int fontSize = 72;

        public event Action GameOverTriggered;

        public bool IsGameOver { get; private set; }

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
            GameOverTriggered?.Invoke();
        }

        private void OnGUI()
        {
            if (!IsGameOver)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.red;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), gameOverText, style);
            GUI.color = previousColor;
        }
    }
}
