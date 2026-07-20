using UnityEngine;

namespace OnlyOnePlayer.Prototype.Core
{
    public sealed class PrototypeGameOverHandler : MonoBehaviour
    {
        [Header("Game Over")]
        [SerializeField] private bool pauseTimeOnGameOver = true;
        [SerializeField] private string gameOverMessage = "Game Over: watcher caught the real player.";

        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        public void GameOver()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            Debug.Log(gameOverMessage, this);

            if (pauseTimeOnGameOver)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
