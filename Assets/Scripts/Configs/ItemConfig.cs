using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Item")]
    public class ItemConfig : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
        public Sprite Icon;
        public ItemType Type;
        public Rarity Rarity;
        public int DamageBonus;
        public int HpBonus;
        public float CritChanceBonus;
        public int SellPrice;
        [TextArea] public string Description;
    }
}
