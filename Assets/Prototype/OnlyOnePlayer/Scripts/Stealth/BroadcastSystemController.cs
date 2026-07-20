using System;
using System.Collections;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class BroadcastSystemController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private bool showBroadcastUi = true;
        [SerializeField, Min(10)] private int fontSize = 28;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

        [Header("Broadcast")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0f)] private float startDelaySeconds = 2f;
        [SerializeField, Min(0f)] private float staticMessageSeconds = 1.25f;
        [SerializeField, Min(0f)] private float instructionSeconds = 4f;
        [SerializeField, Min(0f)] private float endMessageSeconds = 1.25f;

        [Header("Messages")]
        [SerializeField] private string staticMessage = "치지직~";
        [SerializeField] private string instructionMessage = "탐지기에 오류가 나서 잠시 대기해주세요";
        [SerializeField] private string endMessage = "끝났습니다";

        public event Action<BroadcastInstructionType, bool> InstructionStateChanged;

        public BroadcastInstructionType CurrentInstruction { get; private set; } = BroadcastInstructionType.None;
        public bool IsInstructionActive { get; private set; }

        private Coroutine broadcastRoutine;
        private string currentMessage = string.Empty;
        private GUIStyle messageStyle;
        private Texture2D backgroundTexture;

        public void PlayFreezeBroadcast()
        {
            if (broadcastRoutine != null)
            {
                StopCoroutine(broadcastRoutine);
                if (IsInstructionActive)
                {
                    SetInstructionActive(CurrentInstruction, false);
                }
            }

            broadcastRoutine = StartCoroutine(PlayFreezeBroadcastRoutine());
        }

        private void Awake()
        {
            SetText(string.Empty);
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlayFreezeBroadcast();
            }
        }

        private IEnumerator PlayFreezeBroadcastRoutine()
        {
            if (startDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(startDelaySeconds);
            }

            SetText(staticMessage);
            yield return new WaitForSeconds(staticMessageSeconds);

            SetInstructionActive(BroadcastInstructionType.FreezeAllExceptWatchers, true);
            SetText(instructionMessage);
            yield return new WaitForSeconds(instructionSeconds);

            SetInstructionActive(BroadcastInstructionType.FreezeAllExceptWatchers, false);
            SetText(endMessage);
            yield return new WaitForSeconds(endMessageSeconds);

            SetText(string.Empty);
            broadcastRoutine = null;
        }

        private void SetInstructionActive(BroadcastInstructionType instructionType, bool isActive)
        {
            CurrentInstruction = isActive ? instructionType : BroadcastInstructionType.None;
            IsInstructionActive = isActive;
            InstructionStateChanged?.Invoke(instructionType, isActive);
        }

        private void SetText(string message)
        {
            currentMessage = message;
        }

        private void OnGUI()
        {
            if (!showBroadcastUi || string.IsNullOrEmpty(currentMessage))
            {
                return;
            }

            EnsureGuiResources();

            float width = Mathf.Min(Screen.width - 40f, 900f);
            var rect = new Rect((Screen.width - width) * 0.5f, 28f, width, 72f);
            GUI.Label(rect, currentMessage, messageStyle);
        }

        private void EnsureGuiResources()
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = new Texture2D(1, 1)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                backgroundTexture.SetPixel(0, 0, backgroundColor);
                backgroundTexture.Apply();
            }

            if (messageStyle == null)
            {
                messageStyle = new GUIStyle(GUI.skin.label);
                messageStyle.alignment = TextAnchor.MiddleCenter;
                messageStyle.fontSize = fontSize;
                messageStyle.fontStyle = FontStyle.Bold;
                messageStyle.normal.textColor = textColor;
                messageStyle.normal.background = backgroundTexture;
            }
        }
    }
}
