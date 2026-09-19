using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Looks ItemData up by its stable itemId. Save adapters store ids, not
    /// asset references, so restoring needs a way back from id to asset; the
    /// same lookup serves anything else that wants to name an item by id
    /// (shop/quest rewards, ...). Fill it with the "Collect" button on the
    /// asset's inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Item Database", fileName = "Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemData[] items = Array.Empty<ItemData>();

        [NonSerialized] private Dictionary<string, ItemData> byId;

        public IReadOnlyList<ItemData> Items => items;

        public bool TryGet(string itemId, out ItemData item)
        {
            if (byId == null)
            {
                Build();
            }

            item = null;
            return !string.IsNullOrEmpty(itemId) && byId.TryGetValue(itemId, out item);
        }

        /// <summary>Entries with a missing/duplicate id or a null slot - none of these can be saved or looked up reliably.</summary>
        public List<string> FindProblems()
        {
            var problems = new List<string>();
            var seen = new Dictionary<string, ItemData>();

            foreach (var item in items)
            {
                if (item == null)
                {
                    problems.Add("Empty entry in the items list.");
                    continue;
                }

                if (string.IsNullOrEmpty(item.ItemId))
                {
                    problems.Add($"{item.name} has no itemId.");
                }
                else if (seen.TryGetValue(item.ItemId, out var other))
                {
                    problems.Add($"{item.name} and {other.name} share itemId '{item.ItemId}'.");
                }
                else
                {
                    seen[item.ItemId] = item;
                }
            }

            return problems;
        }

        private void Build()
        {
            byId = new Dictionary<string, ItemData>();
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.ItemId))
                {
                    byId.TryAdd(item.ItemId, item);
                }
            }
        }

        private void OnValidate()
        {
            byId = null;
            foreach (var problem in FindProblems())
            {
                Debug.LogWarning($"{nameof(ItemDatabase)} '{name}': {problem}", this);
            }
        }
    }
}
