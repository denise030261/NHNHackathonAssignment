using System.Collections.Generic;
using NHNHackathon.Items;
using UnityEngine;

namespace NHNHackathon.Game
{
    [CreateAssetMenu(
        fileName = "DeveloperModeKeyCatalog",
        menuName = "NHN Hackathon/Developer Mode/Key Catalog")]
    public sealed class DeveloperModeKeyCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> keys = new();
        [SerializeField] private ItemDefinition flashlight;

        public IReadOnlyList<ItemDefinition> Keys => keys;
        public ItemDefinition Flashlight => flashlight;
    }
}
