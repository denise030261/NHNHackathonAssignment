using System;
using DG.Tweening;
using NHNHackathon.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace NHNHackathon.Game
{
    [DisallowMultipleComponent]
    public sealed class EnemyCaptureDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Image fadeOverlay;

        [Header("Camera Look")]
        [SerializeField, Min(0f)] private float lookDuration = 0.35f;
        [SerializeField, Min(0f)] private float cameraShakeDuration = 0.8f;
        [SerializeField, Min(0f)] private float cameraShakeStrength = 5f;
        [SerializeField, Min(1)] private int cameraShakeVibrato = 18;
        [SerializeField, Range(0f, 180f)] private float cameraShakeRandomness = 65f;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDelay = 1.15f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.65f;

        private Sequence sequence;

        public void Play(EnemyController attacker, Action completed)
        {
            sequence?.Kill();
            if (playerCamera == null) playerCamera = Camera.main;
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                Color color = fadeOverlay.color;
                color.a = 0f;
                fadeOverlay.color = color;
            }

            WatcherCapturePresenter presenter =
                attacker.GetComponentInChildren<WatcherCapturePresenter>(true);
            Transform lookTarget = presenter != null && presenter.LookTarget != null
                ? presenter.LookTarget
                : attacker.transform;

            sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (playerCamera != null)
            {
                Vector3 direction = lookTarget.position - playerCamera.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    sequence.Append(playerCamera.transform
                        .DORotateQuaternion(targetRotation, lookDuration)
                        .SetEase(Ease.InOutSine));
                }
                sequence.Append(playerCamera.transform.DOShakeRotation(
                    cameraShakeDuration, cameraShakeStrength,
                    cameraShakeVibrato, cameraShakeRandomness, true));
            }

            if (presenter != null)
            {
                sequence.Insert(lookDuration * 0.5f,
                    presenter.CreateCaptureTween(playerCamera != null
                        ? playerCamera.transform : null));
            }

            if (fadeOverlay != null)
            {
                sequence.Insert(fadeDelay,
                    fadeOverlay.DOFade(1f, fadeDuration).SetEase(Ease.InQuad));
            }
            sequence.OnComplete(() => completed?.Invoke());
        }

        private void OnDisable()
        {
            sequence?.Kill();
        }
    }
}
