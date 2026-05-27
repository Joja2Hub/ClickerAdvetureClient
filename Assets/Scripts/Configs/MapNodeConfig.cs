using UnityEngine;

namespace ClickerAdventure
{
    [CreateAssetMenu(menuName = "RPG Quest/Map Node")]
    public class MapNodeConfig : ScriptableObject
    {
        public string NodeId;
        public string DisplayName;
        public MapNodeType Type;
        public int RequiredLevel = 1;
        public Vector2 MapPosition;
        public DungeonConfig Dungeon;
        public TownConfig Town;
    }
}
