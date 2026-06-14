using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new();
        private static ArtBindingDatabase bindings;

        public static Sprite Background(string id) => Load($"Art/Backgrounds/{id}");
        public static Sprite Hero(string id) => Load($"Art/Characters/Hero/{id}");
        public static Sprite Enemy(string id) => Load($"Art/Characters/Enemies/{id}");
        public static Sprite Reward(string id) => Load($"Art/Rewards/{id}");
        public static Sprite Icon(string id) => Load($"Art/UI/Icons/{id}");
        public static Sprite Panel(string id) => Load($"Art/UI/Panels/{id}");
        public static Sprite Quest(string id) => Load($"Art/Quests/{id}");
        public static Sprite Map(string id) => Load($"Art/Map/{id}");

        public static Sprite EnemyState(string enemyId, string state)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return null;
            }

            var prefix = enemyId.StartsWith("enemy_") ? enemyId : $"enemy_{enemyId}";
            return Enemy($"{prefix}_{state}");
        }

        public static Sprite EnemyIdle(string enemyId) => EnemyState(enemyId, "idle");

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

            bindings ??= Resources.Load<ArtBindingDatabase>("Art/ArtBindings");
            var sprite = bindings != null && bindings.TryGet(path, out var overridden)
                ? overridden
                : Resources.Load<Sprite>(path);
            Cache[path] = sprite;
            return sprite;
        }

        public static void ClearCache()
        {
            Cache.Clear();
            bindings = null;
        }
    }
}
