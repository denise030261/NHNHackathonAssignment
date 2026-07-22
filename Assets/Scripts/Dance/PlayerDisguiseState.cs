using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    public sealed class PlayerDisguiseState : MonoBehaviour
    {
        public event Action<bool> DisguiseStateChanged;

        public bool IsDisguised { get; private set; }

        public void SetDisguised(bool value)
        {
            if (IsDisguised == value)
            {
                return;
            }

            IsDisguised = value;
            DisguiseStateChanged?.Invoke(value);
        }

        private void OnDisable()
        {
            SetDisguised(false);
        }
    }
}
