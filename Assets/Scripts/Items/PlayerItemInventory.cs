using System;
using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemInventory : MonoBehaviour
    {
        private readonly HashSet<string> collectedItemIds = new HashSet<string>();
        private readonly List<ItemDefinition> collectedItems = new List<ItemDefinition>();

        public event Action InventoryChanged;
        public IReadOnlyList<ItemDefinition> Items => collectedItems;

        public bool TryCollect(ItemDefinition item)
        {
            if (item == null
                || string.IsNullOrWhiteSpace(item.ItemId)
                || !collectedItemIds.Add(item.ItemId))
            {
                return false;
            }

            collectedItems.Add(item);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool Contains(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && collectedItemIds.Contains(itemId);
        }
    }
}
