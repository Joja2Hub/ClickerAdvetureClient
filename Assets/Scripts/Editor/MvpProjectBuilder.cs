#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClickerAdventure.EditorTools
{
    [InitializeOnLoad]
    public static class MvpProjectBuilder
    {
        static MvpProjectBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        private static void BuildIfMissing()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (File.Exists("Assets/Scenes/00_Boot.unity"))
            {
                return;
            }

            Build();
        }

        [MenuItem("RPG Quest/Build MVP Project")]
        public static void Build()
        {
            EnsureFolders();
            var slime = CreateEnemy("slime_green", "Лесной слайм", 34, 5, 1.8f, 10, 6);
            var bat = CreateEnemy("cave_bat", "Пещерная летучая мышь", 26, 7, 1.4f, 12, 7);
            var golem = CreateEnemy("stone_golem", "Каменный голем", 70, 12, 2.3f, 28, 16);
            var loot = CreateLootTable();
            var town = CreateTown();
            var forest = CreateDungeon("sunny_forest", "Солнечный лес", 1, 2, 2, 20, 12, loot, slime, bat);
            var cave = CreateDungeon("crystal_cave", "Кристальная пещера", 3, 2, 2, 45, 22, loot, bat, golem);
            CreateMapNode("sunny_forest", "Солнечный лес", MapNodeType.Dungeon, 1, new Vector2(-260, 240), forest, null);
            CreateMapNode("home_village", "Домашняя деревня", MapNodeType.Village, 1, new Vector2(220, 80), null, town);
            CreateMapNode("crystal_cave", "Кристальная пещера", MapNodeType.Dungeon, 3, new Vector2(40, -230), cave, null);
            CreateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("RPG Quest MVP project generated.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/ScriptableObjects/Enemies",
                "Assets/ScriptableObjects/Dungeons",
                "Assets/ScriptableObjects/MapNodes",
                "Assets/ScriptableObjects/LootTables",
                "Assets/ScriptableObjects/Towns",
                "Assets/Scenes"
            };

            foreach (var folder in folders)
            {
                Directory.CreateDirectory(folder);
            }
        }

        private static EnemyConfig CreateEnemy(string id, string displayName, int hp, int damage, float interval, int xp, int gold)
        {
            var path = $"Assets/ScriptableObjects/Enemies/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EnemyId = id;
            asset.DisplayName = displayName;
            asset.MaxHp = hp;
            asset.Damage = damage;
            asset.AttackInterval = interval;
            asset.XpReward = xp;
            asset.GoldReward = gold;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static LootTableConfig CreateLootTable()
        {
            const string path = "Assets/ScriptableObjects/LootTables/starter_loot.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LootTableConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LootTableConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Entries.Clear();
            asset.Entries.Add(new LootEntry { ItemId = "wooden_sword", Weight = 2 });
            asset.Entries.Add(new LootEntry { ItemId = "shiny_pebble", Weight = 4 });
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DungeonConfig CreateDungeon(string id, string displayName, int level, int waves, int perWave, int xp, int gold, LootTableConfig loot, params EnemyConfig[] enemies)
        {
            var path = $"Assets/ScriptableObjects/Dungeons/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DungeonConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DungeonConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.DungeonId = id;
            asset.DisplayName = displayName;
            asset.BiomeId = "starter";
            asset.RequiredLevel = level;
            asset.WavesCount = waves;
            asset.EnemiesPerWave = perWave;
            asset.BaseXpReward = xp;
            asset.BaseGoldReward = gold;
            asset.LootTable = loot;
            asset.EnemyPool.Clear();
            asset.EnemyPool.AddRange(enemies);
            asset.Boss = enemies.Length > 0 ? enemies[^1] : null;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static TownConfig CreateTown()
        {
            const string path = "Assets/ScriptableObjects/Towns/home_village.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TownConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TownConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.TownId = "home_village";
            asset.DisplayName = "Домашняя деревня";
            asset.Description = "Место, где реальные добрые дела становятся приключениями.";
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static MapNodeConfig CreateMapNode(string id, string displayName, MapNodeType type, int level, Vector2 position, DungeonConfig dungeon, TownConfig town)
        {
            var path = $"Assets/ScriptableObjects/MapNodes/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<MapNodeConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MapNodeConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.NodeId = id;
            asset.DisplayName = displayName;
            asset.Type = type;
            asset.RequiredLevel = level;
            asset.MapPosition = position;
            asset.Dungeon = dungeon;
            asset.Town = town;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateScenes()
        {
            CreateBootScene();
            CreateControllerScene<LoginScreen>("01_Login", "LoginScreen");
            CreateMapScene();
            CreateControllerScene<BattleScreen>("03_Battle", "BattleScreen");
            CreateControllerScene<TownScreen>("04_Town", "TownScreen");
            CreateEmptyScene("05_Loading", "Loading");

            var scenePaths = new[]
            {
                "Assets/Scenes/00_Boot.unity",
                "Assets/Scenes/01_Login.unity",
                "Assets/Scenes/02_Map.unity",
                "Assets/Scenes/03_Battle.unity",
                "Assets/Scenes/04_Town.unity",
                "Assets/Scenes/05_Loading.unity"
            };

            var buildScenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (var i = 0; i < scenePaths.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            var root = new GameObject("GameBootstrap");
            root.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/00_Boot.unity");
        }

        private static void CreateMapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            var root = new GameObject("MapScreen");
            var screen = root.AddComponent<MapScreen>();
            var nodes = AssetDatabase.FindAssets("t:MapNodeConfig", new[] { "Assets/ScriptableObjects/MapNodes" });
            var configs = new MapNodeConfig[nodes.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                configs[i] = AssetDatabase.LoadAssetAtPath<MapNodeConfig>(AssetDatabase.GUIDToAssetPath(nodes[i]));
            }

            var serialized = new SerializedObject(screen);
            var property = serialized.FindProperty("nodes");
            property.arraySize = configs.Length;
            for (var i = 0; i < configs.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/02_Map.unity");
        }

        private static void CreateControllerScene<T>(string sceneName, string rootName) where T : Component
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            var root = new GameObject(rootName);
            root.AddComponent<T>();
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
        }

        private static void CreateEmptyScene(string sceneName, string rootName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            new GameObject(rootName);
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
        }

        private static void AddCamera()
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }
    }
}
#endif
