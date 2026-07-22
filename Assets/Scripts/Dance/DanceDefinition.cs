using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [Serializable]
    public sealed class DanceDefinition
    {
        [SerializeField, Min(1), Tooltip("Unique number used by AI sequences and player input.")]
        private int id = 1;

        [SerializeField, Tooltip("Designer-facing name of this dance.")]
        private string danceName = "New Dance";

        [SerializeField, Tooltip("Temporary colour used until an animation is available.")]
        private Color displayColor = Color.white;

        public int Id => id;
        public string DanceName => danceName;
        public Color DisplayColor => displayColor;
    }
}
