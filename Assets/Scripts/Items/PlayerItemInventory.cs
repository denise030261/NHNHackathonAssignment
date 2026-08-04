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

        public bool Contains(ItemDefinition item)
        {
            return item != null && Contains(item.ItemId);
        }

        public bool TryConsume(IReadOnlyList<ItemDefinition> items)
        {
            if (items == null)
            {
                return false;
            }

            for (int index = 0; index < items.Count; index++)
            {
                ItemDefinition item = items[index];
                if (item == null || !Contains(item))
                {
                    return false;
                }
            }

            for (int index = 0; index < items.Count; index++)
            {
                ItemDefinition item = items[index];
                if (collectedItemIds.Remove(item.ItemId))
                {
                    collectedItems.RemoveAll(value => value != null && value.ItemId == item.ItemId);
                }
            }

            InventoryChanged?.Invoke();
            return true;
        }
    }
}
