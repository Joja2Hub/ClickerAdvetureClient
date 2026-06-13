using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Art Binding Database")]
    public sealed class ArtBindingDatabase : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, Sprite> lookup;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(string key, out Sprite sprite)
        {
            EnsureLookup();
            return lookup.TryGetValue(key, out sprite) && sprite != null;
        }

        public void Set(string key, Sprite sprite)
        {
            var entry = entries.Find(item => item.Key == key);
            if (entry == null)
            {
                entry = new Entry { Key = key };
                entries.Add(entry);
            }

            entry.Sprite = sprite;
            lookup = null;
        }

        public void Remove(string key)
        {
            entries.RemoveAll(item => item.Key == key);
            lookup = null;
        }

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Sprite != null)
                {
                    lookup[entry.Key] = entry.Sprite;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lookup = null;
        }
#endif
    }
}
