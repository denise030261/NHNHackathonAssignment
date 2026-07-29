using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemInventory : MonoBehaviour
    {
        private readonly HashSet<string> collectedItemIds = new HashSet<string>();

        public bool TryCollect(ItemDefinition item)
        {
            return item != null
                   && !string.IsNullOrWhiteSpace(item.ItemId)
                   && collectedItemIds.Add(item.ItemId);
        }

        public bool Contains(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && collectedItemIds.Contains(itemId);
        }
    }
}
