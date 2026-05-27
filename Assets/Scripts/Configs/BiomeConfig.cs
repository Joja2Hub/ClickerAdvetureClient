using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Biome")]
    public class BiomeConfig : ScriptableObject
    {
        public string BiomeId;
        public string DisplayName;
        public Sprite Background;
        public List<EnemyConfig> EnemyPool = new();
        public LootTableConfig LootTable;
    }
}
