using System.Collections.Generic;
using DG.Tweening;
using NHNHackathon.Characters;
using NHNHackathon.Dance;
using NHNHackathon.Interaction;
using NHNHackathon.LightSystem;
using NHNHackathon.SaveSystem;
using UnityEngine;

namespace NHNHackathon.Cinematics
{
    [DisallowMultipleComponent]
    public sealed class OpeningWakeUpController : MonoBehaviour
    {
        [Header("Eyelid UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform topEyelid;
        [SerializeField] private RectTransform bottomEyelid;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float initialBlackDuration = 1f;
        [SerializeField, Min(0.01f)] private float firstBlinkOpenDuration = 0.45f;
        [SerializeField, Range(0.05f, 0.9f)] private float firstBlinkOpenRatio = 0.28f;
        [SerializeField, Min(0.01f)] private float firstBlinkCloseDuration = 0.35f;
        [SerializeField, Min(0f)] private float closedPauseDuration = 0.3f;
        [SerializeField, Min(0.01f)] private float finalOpenDuration = 2.2f;

        [Header("Head Lift")]
        [SerializeField, Range(0f, 90f)] private float startingPitch = -48f;
        [SerializeField, Range(-30f, 0f)] private float endingPitch;

        [Header("Player Controls")]
        [SerializeField, Tooltip("Optional. Movement, interaction, dance, and flashlight are found automatically when empty.")]
        private Behaviour[] controlledBehaviours;

        private readonly List<Behaviour> lockedControls = new();
        private readonly List<bool> previousControlStates = new();
        private PlayerCameraController cameraController;
        private Sequence sequence;
        private bool controlsAreLocked;

        private void Start()
        {
            if (CheckpointSession.LastLoadWasRespawn)
            {
                gameObject.SetActive(false);
                return;
            }

            cameraController = FindAnyObjectByType<PlayerCameraController>();
            LockPlayerControls();
            PrepareVisuals();
            PlaySequence();
        }

        private void PrepareVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = false;
            }
            if (topEyelid != null) topEyelid.anchoredPosition = Vector2.zero;
            if (bottomEyelid != null) bottomEyelid.anchoredPosition = Vector2.zero;

            if (cameraController != null)
            {
                cameraController.RequestPerspective(CameraPerspective.FirstPerson, 0f);
                cameraController.RequestPitchAnimation(startingPitch, startingPitch, 0f);
            }
        }

        private void PlaySequence()
        {
            if (topEyelid == null || bottomEyelid == null)
            {
                CompleteSequence();
                return;
            }

            Canvas.ForceUpdateCanvases();
            float topTravel = Mathf.Max(1f, topEyelid.rect.height + 8f);
            float bottomTravel = Mathf.Max(1f, bottomEyelid.rect.height + 8f);
            float headLiftStart = initialBlackDuration + firstBlinkOpenDuration
                + firstBlinkCloseDuration + closedPauseDuration;

            sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            sequence.AppendInterval(initialBlackDuration);
            sequence.Append(topEyelid.DOAnchorPosY(
                topTravel * firstBlinkOpenRatio, firstBlinkOpenDuration).SetEase(Ease.OutSine));
            sequence.Join(bottomEyelid.DOAnchorPosY(
                -bottomTravel * firstBlinkOpenRatio, firstBlinkOpenDuration).SetEase(Ease.OutSine));
            sequence.Append(topEyelid.DOAnchorPosY(0f, firstBlinkCloseDuration).SetEase(Ease.InSine));
            sequence.Join(bottomEyelid.DOAnchorPosY(0f, firstBlinkCloseDuration).SetEase(Ease.InSine));
            sequence.AppendInterval(closedPauseDuration);
            sequence.InsertCallback(headLiftStart, () =>
                cameraController?.RequestPitchAnimation(
                    startingPitch, endingPitch, finalOpenDuration));
            sequence.Append(topEyelid.DOAnchorPosY(topTravel, finalOpenDuration)
                .SetEase(Ease.InOutSine));
            sequence.Join(bottomEyelid.DOAnchorPosY(-bottomTravel, finalOpenDuration)
                .SetEase(Ease.InOutSine));
            sequence.OnComplete(CompleteSequence);
        }

        private void LockPlayerControls()
        {
            lockedControls.Clear();
            previousControlStates.Clear();

            if (controlledBehaviours != null && controlledBehaviours.Length > 0)
            {
                foreach (Behaviour behaviour in controlledBehaviours)
                {
                    AddControl(behaviour);
                }
            }
            else
            {
                AddControl(FindAnyObjectByType<PlayerMovement>());
                AddControl(FindAnyObjectByType<PlayerInteractor>());
                AddControl(FindAnyObjectByType<PlayerDanceInput>());
                AddControl(FindAnyObjectByType<PlayerFlashlightController>());
            }

            foreach (Behaviour behaviour in lockedControls)
            {
                previousControlStates.Add(behaviour.enabled);
                behaviour.enabled = false;
            }
            controlsAreLocked = true;
        }

        private void AddControl(Behaviour behaviour)
        {
            if (behaviour != null && behaviour != cameraController && !lockedControls.Contains(behaviour))
            {
                lockedControls.Add(behaviour);
            }
        }

        private void CompleteSequence()
        {
            RestorePlayerControls();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void RestorePlayerControls()
        {
            if (!controlsAreLocked)
            {
                return;
            }
            for (int index = 0; index < lockedControls.Count; index++)
            {
                if (lockedControls[index] != null)
                {
                    lockedControls[index].enabled = previousControlStates[index];
                }
            }
            controlsAreLocked = false;
        }

        private void OnDisable()
        {
            sequence?.Kill();
            RestorePlayerControls();
        }

        private void OnValidate()
        {
            initialBlackDuration = Mathf.Max(0f, initialBlackDuration);
            firstBlinkOpenDuration = Mathf.Max(0.01f, firstBlinkOpenDuration);
            firstBlinkCloseDuration = Mathf.Max(0.01f, firstBlinkCloseDuration);
            closedPauseDuration = Mathf.Max(0f, closedPauseDuration);
            finalOpenDuration = Mathf.Max(0.01f, finalOpenDuration);
        }
    }
}
