using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [CreateAssetMenu(fileName = "DanceCatalog", menuName = "NHN Hackathon/Dance/Dance Catalog")]
    public sealed class DanceCatalog : ScriptableObject
    {
        [SerializeField, Tooltip("Single source of truth for every dance used by the game.")]
        private List<DanceDefinition> dances = new List<DanceDefinition>();

        public IReadOnlyList<DanceDefinition> Dances => dances;

        public bool TryGetDance(int danceId, out DanceDefinition dance)
        {
            dance = null;
            if (dances == null)
            {
                return false;
            }

            foreach (DanceDefinition candidate in dances)
            {
                if (candidate != null && candidate.Id == danceId)
                {
                    dance = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            HashSet<int> usedIds = new HashSet<int>();
            foreach (DanceDefinition dance in dances)
            {
                if (dance != null && !usedIds.Add(dance.Id))
                {
                    Debug.LogWarning($"Dance Catalog contains duplicate ID {dance.Id}.", this);
                }
            }
        }
    }
}
