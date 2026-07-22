using System;
using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [Serializable]
    public sealed class DanceInputBinding
    {
        [SerializeField] private KeyCode key = KeyCode.Alpha1;
        [SerializeField, Min(1)] private int danceId = 1;

        public KeyCode Key => key;
        public int DanceId => danceId;
    }

    [CreateAssetMenu(fileName = "DanceInputMapping", menuName = "NHN Hackathon/Dance/Input Mapping")]
    public sealed class DanceInputMapping : ScriptableObject
    {
        [SerializeField, Tooltip("Editable key-to-dance mappings checked in list order.")]
        private List<DanceInputBinding> bindings = new List<DanceInputBinding>();

        public IReadOnlyList<DanceInputBinding> Bindings => bindings;
    }
}
