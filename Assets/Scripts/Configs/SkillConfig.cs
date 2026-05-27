using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Skill")]
    public class SkillConfig : ScriptableObject
    {
        public string SkillId;
        public string DisplayName;
        public SkillType Type;
        public Sprite Icon;
        public float Cooldown = 5f;
        public int Power = 30;
        [TextArea] public string Description;
    }
}
