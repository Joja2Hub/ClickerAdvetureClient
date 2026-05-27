using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Town")]
    public class TownConfig : ScriptableObject
    {
        public string TownId;
        public string DisplayName;
        [TextArea] public string Description;
        public bool HasMerchant = true;
        public bool HasAdventureGuild = true;
        public bool HasBlacksmith = true;
        public bool HasSkillTrainer = true;
        public bool HasAlchemist = true;
    }
}
