using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NHNHackathon.Characters
{
    [DisallowMultipleComponent]
    public sealed class PlayerPerspectiveVisualController : MonoBehaviour
    {
        private sealed class RendererState
        {
            public Renderer Renderer;
            public ShadowCastingMode OriginalShadowMode;
        }

        [Header("References")]
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField, Tooltip("The animated third-person character model. All of its renderers are hidden in first person.")]
        private Transform characterModelRoot;

        [Header("First Person")]
        [SerializeField, Tooltip("Keeps the player shadow while preventing the body and arms from rendering in the first-person camera.")]
        private bool keepShadowsInFirstPerson = true;

        private readonly List<RendererState> rendererStates = new();
        private bool? isModelVisible;

        private void Awake()
        {
            ResolveReferences();
            CacheRenderers();
            ApplyPerspectiveVisibility(true);
        }

        private void LateUpdate()
        {
            ApplyPerspectiveVisibility(false);
        }

        private void ResolveReferences()
        {
            cameraController ??= GetComponent<PlayerCameraController>();
            characterModelRoot ??= transform.Find("CharacterModel");
        }

        private void CacheRenderers()
        {
            rendererStates.Clear();
            if (characterModelRoot == null)
            {
                return;
            }

            foreach (Renderer renderer in
                     characterModelRoot.GetComponentsInChildren<Renderer>(true))
            {
                rendererStates.Add(new RendererState
                {
                    Renderer = renderer,
                    OriginalShadowMode = renderer.shadowCastingMode
                });
            }
        }

        private void ApplyPerspectiveVisibility(bool force)
        {
            if (cameraController == null)
            {
                return;
            }

            // Show as soon as a third-person transition starts. During the return
            // transition, keep the model visible until the camera fully reaches
            // first person so it does not disappear halfway through the blend.
            bool shouldShowModel =
                cameraController.Perspective == CameraPerspective.ThirdPerson
                || cameraController.IsTransitioning;
            if (!force && isModelVisible == shouldShowModel)
            {
                return;
            }

            isModelVisible = shouldShowModel;
            foreach (RendererState state in rendererStates)
            {
                if (state.Renderer == null)
                {
                    continue;
                }

                state.Renderer.shadowCastingMode = shouldShowModel
                    ? state.OriginalShadowMode
                    : GetFirstPersonShadowMode(state.OriginalShadowMode);
            }
        }

        private ShadowCastingMode GetFirstPersonShadowMode(
            ShadowCastingMode originalMode)
        {
            if (!keepShadowsInFirstPerson || originalMode == ShadowCastingMode.Off)
            {
                return ShadowCastingMode.Off;
            }

            return ShadowCastingMode.ShadowsOnly;
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
