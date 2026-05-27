using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Enemy")]
    public class EnemyConfig : ScriptableObject
    {
        public string EnemyId;
        public string DisplayName;
        public Sprite Sprite;
        public int MaxHp = 30;
        public int Damage = 5;
        public float AttackInterval = 2f;
        public float MoveSpeed = 1f;
        public int XpReward = 10;
        public int GoldReward = 5;
        public LootTableConfig LootTable;
    }
}
