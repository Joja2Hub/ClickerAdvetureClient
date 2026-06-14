using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure.EditorTools
{
    public sealed class GraphArtImporter : AssetPostprocessor
    {
        private static readonly HashSet<string> PendingPrefabSync = new();
        private static bool syncScheduled;

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

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets.Concat(movedAssets))
            {
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    PendingPrefabSync.Add(path);
                }
            }

            if (PendingPrefabSync.Count == 0 || syncScheduled)
            {
                return;
            }

            syncScheduled = true;
            EditorApplication.delayCall += SyncImportedPrefabs;
        }

        private static void SyncImportedPrefabs()
        {
            syncScheduled = false;
            var paths = PendingPrefabSync.ToArray();
            PendingPrefabSync.Clear();
            var changed = paths.Aggregate(
                false,
                (current, path) => ManualArtPrefabSynchronizer.SyncPrefab(path) || current);
            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
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
            ManualArtPrefabSynchronizer.SyncAll();
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
                var prefabPath = $"{prefabFolder}/{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    continue;
                }

                var go = new GameObject(name);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = null;
                renderer.sortingOrder = 10;
                go.transform.localScale = Vector3.one * scale;
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void CreateUiSlots(string[] names, string prefabFolder, Vector2 size)
        {
            foreach (var name in names)
            {
                var prefabPath = $"{prefabFolder}/{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    continue;
                }

                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = size;
                var image = go.GetComponent<Image>();
                image.sprite = null;
                image.preserveAspect = true;
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
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

    public sealed class ManualArtSaveProcessor : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            var changed = false;
            foreach (var path in paths.Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
            {
                changed |= ManualArtPrefabSynchronizer.SyncPrefab(path);
            }

            if (!changed)
            {
                return paths;
            }

            return paths
                .Append(ManualArtPrefabSynchronizer.DatabasePath)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }

    public static class ManualArtPrefabSynchronizer
    {
        public const string DatabasePath = "Assets/Resources/Art/ArtBindings.asset";
        private const string InitialSyncSessionKey = "ClickerAdventure.ManualArt.InitialSync";

        private static readonly (string PrefabFolder, string RuntimeFolder)[] Mappings =
        {
            ("Assets/Prefabs/Art/Characters/Hero/", "Art/Characters/Hero"),
            ("Assets/Prefabs/Art/Characters/Enemies/", "Art/Characters/Enemies"),
            ("Assets/Prefabs/Art/Map/", "Art/Map"),
            ("Assets/Prefabs/Art/Town/", "Art/Town"),
            ("Assets/Prefabs/Art/Quests/", "Art/Quests"),
            ("Assets/Prefabs/UI/Generated/Icons/", "Art/UI/Icons"),
            ("Assets/Prefabs/UI/Generated/Panels/", "Art/UI/Panels"),
            ("Assets/Prefabs/UI/Generated/Rewards/", "Art/Rewards")
        };

        [InitializeOnLoadMethod]
        private static void ScheduleInitialSync()
        {
            if (SessionState.GetBool(InitialSyncSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(InitialSyncSessionKey, true);
            EditorApplication.delayCall += SyncAll;
        }

        [MenuItem("RPG Quest/Art/Sync Prefabs To Runtime")]
        public static void SyncAll()
        {
            var changed = false;
            foreach (var mapping in Mappings)
            {
                var folder = mapping.PrefabFolder.TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    changed |= SyncPrefab(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("Manual art prefabs synchronized with runtime bindings.");
            }
        }

        public static bool SyncPrefab(string prefabPath)
        {
            if (!TryGetRuntimeKey(prefabPath, out var runtimeKey))
            {
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return false;
            }

            var sprite = prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
            sprite ??= prefab.GetComponentInChildren<Image>(true)?.sprite;

            var database = AssetDatabase.LoadAssetAtPath<ArtBindingDatabase>(DatabasePath);
            if (database == null)
            {
                return false;
            }

            var hadValue = database.TryGet(runtimeKey, out var previous);
            if (sprite == null)
            {
                if (!hadValue)
                {
                    return false;
                }

                database.Remove(runtimeKey);
            }
            else
            {
                if (hadValue && previous == sprite)
                {
                    return false;
                }

                database.Set(runtimeKey, sprite);
            }

            EditorUtility.SetDirty(database);
            ArtLibrary.ClearCache();
            return true;
        }

        private static bool TryGetRuntimeKey(string prefabPath, out string runtimeKey)
        {
            foreach (var mapping in Mappings)
            {
                if (!prefabPath.StartsWith(mapping.PrefabFolder, StringComparison.Ordinal))
                {
                    continue;
                }

                runtimeKey = $"{mapping.RuntimeFolder}/{Path.GetFileNameWithoutExtension(prefabPath)}";
                return true;
            }

            runtimeKey = null;
            return false;
        }
    }
}
