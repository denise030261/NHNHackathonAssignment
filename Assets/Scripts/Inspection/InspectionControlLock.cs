using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Inspection
{
    [DisallowMultipleComponent]
    public sealed class InspectionControlLock : MonoBehaviour
    {
        [SerializeField, Tooltip("Player input behaviours disabled while inspection is open.")]
        private Behaviour[] controlledBehaviours;
        [SerializeField, Tooltip("Also pauses enemies and other world simulation.")]
        private bool pauseWorldDuringInspection = true;

        private readonly List<bool> previousEnabledStates = new List<bool>();
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private float previousTimeScale;
        private bool isLocked;

        public void Lock()
        {
            if (isLocked)
            {
                return;
            }

            isLocked = true;
            previousEnabledStates.Clear();
            foreach (Behaviour behaviour in controlledBehaviours)
            {
                previousEnabledStates.Add(behaviour != null && behaviour.enabled);
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            previousTimeScale = Time.timeScale;
            if (pauseWorldDuringInspection)
            {
                Time.timeScale = 0f;
            }
        }

        public void Unlock()
        {
            if (!isLocked)
            {
                return;
            }

            if (pauseWorldDuringInspection)
            {
                Time.timeScale = previousTimeScale;
            }

            for (int index = 0; index < controlledBehaviours.Length; index++)
            {
                Behaviour behaviour = controlledBehaviours[index];
                if (behaviour != null)
                {
                    behaviour.enabled = previousEnabledStates[index];
                }
            }

            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            isLocked = false;
        }

        private void OnDisable()
        {
            Unlock();
        }
    }
}
