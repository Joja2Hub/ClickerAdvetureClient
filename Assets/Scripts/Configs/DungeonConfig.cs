using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Dungeon")]
    public class DungeonConfig : ScriptableObject
    {
        public string DungeonId;
        public string DisplayName;
        public string BiomeId;
        public int RequiredLevel = 1;
        public int WavesCount = 2;
        public int EnemiesPerWave = 2;
        public List<EnemyConfig> EnemyPool = new();
        public EnemyConfig Boss;
        public int BaseXpReward = 20;
        public int BaseGoldReward = 12;
        public LootTableConfig LootTable;
    }
}
