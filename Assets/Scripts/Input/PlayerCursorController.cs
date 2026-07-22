using UnityEngine;

namespace NHNHackathon.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerCursorController : MonoBehaviour
    {
        [Header("Cursor")]
        [SerializeField, Tooltip("Lock and hide the cursor when gameplay begins.")]
        private bool lockCursorOnStart = true;

        private void Start()
        {
            if (lockCursorOnStart)
            {
                LockCursor();
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }
            else if (UnityEngine.Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                UnlockCursor();
            }
        }
    }
}
