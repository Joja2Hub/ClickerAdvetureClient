using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Background(string id) => Load($"Art/Backgrounds/{id}");
        public static Sprite Hero(string id) => Load($"Art/Characters/Hero/{id}");
        public static Sprite Enemy(string id) => Load($"Art/Characters/Enemies/{id}");
        public static Sprite Item(string id) => Load($"Art/Items/{id}");
        public static Sprite Icon(string id) => Load($"Art/UI/Icons/{id}");
        public static Sprite Panel(string id) => Load($"Art/UI/Panels/{id}");
        public static Sprite Quest(string id) => Load($"Art/Quests/{id}");
        public static Sprite Map(string id) => Load($"Art/Map/{id}");

        public static Sprite EnemyIdle(string enemyId)
        {
            return enemyId switch
            {
                "slime_green" => Enemy("enemy_slime_green_idle"),
                "cave_bat" => Enemy("enemy_cave_bat_idle"),
                "stone_golem" => Enemy("enemy_stone_golem_idle"),
                _ => null
            };
        }

        public static Sprite DungeonBackground(string dungeonId)
        {
            return dungeonId switch
            {
                "crystal_cave" => Background("bg_battle_crystal_cave"),
                "sunny_forest" => Background("bg_battle_sunny_forest"),
                _ => Background("bg_battle_sunny_forest")
            };
        }

        private static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(path);
            Cache[path] = sprite;
            return sprite;
        }
    }
}
