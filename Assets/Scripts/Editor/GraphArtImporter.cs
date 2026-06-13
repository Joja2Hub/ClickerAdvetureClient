using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure.EditorTools
{
    public sealed class GraphArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/") && !assetPath.StartsWith("Assets/Art/SourceSheets/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = assetPath.StartsWith("Assets/Art/SourceSheets/")
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = assetPath.Contains("/Backgrounds/") ? 4096 : 2048;
        }
    }

    public static class GraphArtBuilder
    {
        private static readonly string[] HeroSlots =
        {
            "char_child_hero_idle", "char_child_hero_run", "char_child_hero_attack",
            "char_child_hero_hurt", "char_child_hero_block", "char_child_hero_magic",
            "char_child_hero_victory", "char_child_hero_defeat", "char_child_hero_portrait"
        };

        private static readonly string[] EnemySlots =
        {
            "enemy_blue_slime_idle", "enemy_blue_slime_attack", "enemy_blue_slime_hurt", "enemy_blue_slime_defeat",
            "enemy_cave_bat_idle", "enemy_cave_bat_attack", "enemy_cave_bat_hurt", "enemy_cave_bat_defeat",
            "enemy_mosquito_idle", "enemy_mosquito_attack", "enemy_mosquito_hurt", "enemy_mosquito_defeat",
            "enemy_poison_slime_idle", "enemy_poison_slime_attack", "enemy_poison_slime_hurt", "enemy_poison_slime_defeat",
            "enemy_sand_crab_idle", "enemy_sand_crab_attack", "enemy_sand_crab_hurt", "enemy_sand_crab_defeat",
            "enemy_sand_spirit_idle", "enemy_sand_spirit_attack", "enemy_sand_spirit_hurt", "enemy_sand_spirit_defeat",
            "enemy_shell_guardian_idle", "enemy_shell_guardian_attack", "enemy_shell_guardian_hurt", "enemy_shell_guardian_defeat",
            "enemy_slime_green_idle", "enemy_slime_green_attack", "enemy_slime_green_hurt", "enemy_slime_green_defeat",
            "enemy_stone_golem_idle", "enemy_stone_golem_attack", "enemy_stone_golem_hurt", "enemy_stone_golem_defeat",
            "enemy_crystal_spider", "enemy_forest_mushroom", "enemy_leaf_imp", "enemy_moss_wolf",
            "enemy_shadow_wisp", "enemy_tiny_mimic"
        };

        private static readonly string[] UiIconSlots =
        {
            "ui_icon_attack", "ui_icon_back", "ui_icon_bag", "ui_icon_defend",
            "ui_icon_guild", "ui_icon_home", "ui_icon_lock", "ui_icon_map",
            "ui_icon_refresh", "ui_icon_settings"
        };

        private static readonly string[] UiPanelSlots =
        {
            "rarity_frame_common", "rarity_frame_uncommon", "rarity_frame_rare",
            "rarity_frame_epic", "rarity_frame_legendary", "ui_badge_approved",
            "ui_badge_assigned", "ui_badge_cancelled", "ui_badge_claimed",
            "ui_badge_locked", "ui_badge_rejected", "ui_badge_submitted",
            "ui_button_danger", "ui_button_primary", "ui_button_secondary",
            "ui_button_success", "ui_frame_bronze", "ui_frame_gold",
            "ui_frame_silver", "ui_input_field", "ui_panel_dark_wood",
            "ui_panel_parchment", "ui_panel_quest_card", "ui_panel_slot"
        };

        private static readonly string[] RewardSlots = { "reward_gold", "reward_xp" };

        [MenuItem("RPG Quest/Art/Rebuild Manual Art Prefabs %#m")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            EnsureFolders();
            RemoveObsoleteArt();
            CreateSpriteRendererSlots(HeroSlots, "Assets/Prefabs/Art/Characters/Hero", 1.8f);
            CreateSpriteRendererSlots(EnemySlots, "Assets/Prefabs/Art/Characters/Enemies", 1.8f);
            CreateUiSlots(UiIconSlots, "Assets/Prefabs/UI/Generated/Icons", new Vector2(96, 96));
            CreateUiSlots(UiPanelSlots, "Assets/Prefabs/UI/Generated/Panels", new Vector2(320, 160));
            CreateUiSlots(RewardSlots, "Assets/Prefabs/UI/Generated/Rewards", new Vector2(72, 72));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Manual art prefab slots rebuilt. FX and inventory item prefabs were removed.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs/Art");
            EnsureFolder("Assets/Prefabs/Art/Characters");
            EnsureFolder("Assets/Prefabs/Art/Characters/Hero");
            EnsureFolder("Assets/Prefabs/Art/Characters/Enemies");
            EnsureFolder("Assets/Prefabs/Art/Map");
            EnsureFolder("Assets/Prefabs/Art/Town");
            EnsureFolder("Assets/Prefabs/Art/Quests");
            EnsureFolder("Assets/Prefabs/UI/Generated");
            EnsureFolder("Assets/Prefabs/UI/Generated/Icons");
            EnsureFolder("Assets/Prefabs/UI/Generated/Panels");
            EnsureFolder("Assets/Prefabs/UI/Generated/Rewards");
        }

        private static void RemoveObsoleteArt()
        {
            AssetDatabase.DeleteAsset("Assets/Prefabs/Art/FX");
            AssetDatabase.DeleteAsset("Assets/Prefabs/Art/Items");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Art/Quests" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path).StartsWith("fx_", StringComparison.Ordinal))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static void CreateSpriteRendererSlots(string[] names, string prefabFolder, float scale)
        {
            foreach (var name in names)
            {
                var go = new GameObject(name);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = null;
                renderer.sortingOrder = 10;
                go.transform.localScale = Vector3.one * scale;
                PrefabUtility.SaveAsPrefabAsset(go, $"{prefabFolder}/{name}.prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void CreateUiSlots(string[] names, string prefabFolder, Vector2 size)
        {
            foreach (var name in names)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = size;
                var image = go.GetComponent<Image>();
                image.sprite = null;
                image.preserveAspect = true;
                PrefabUtility.SaveAsPrefabAsset(go, $"{prefabFolder}/{name}.prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
