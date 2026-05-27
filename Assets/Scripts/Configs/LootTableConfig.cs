using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Loot Table")]
    public class LootTableConfig : ScriptableObject
    {
        public List<LootEntry> Entries = new();

        public string Roll()
        {
            if (Entries.Count == 0)
            {
                return string.Empty;
            }

            var total = 0f;
            foreach (var entry in Entries)
            {
                total += Mathf.Max(0f, entry.Weight);
            }

            var roll = Random.value * total;
            foreach (var entry in Entries)
            {
                roll -= Mathf.Max(0f, entry.Weight);
                if (roll <= 0f)
                {
                    return entry.ItemId;
                }
            }

            return Entries[^1].ItemId;
        }
    }

    [System.Serializable]
    public class LootEntry
    {
        public string ItemId;
        public float Weight = 1f;
    }
}
