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
            importer.spriteImportMode = SpriteImportMode.Single;
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
        [MenuItem("RPG Quest/Art/Rebuild Graph Art Prefabs")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            EnsureFolders();
            CreateSpriteRendererPrefabs("Assets/Resources/Art/Characters/Hero", "Assets/Prefabs/Art/Characters/Hero", 1.8f);
            CreateSpriteRendererPrefabs("Assets/Resources/Art/Characters/Enemies", "Assets/Prefabs/Art/Characters/Enemies", 1.8f);
            CreateSpriteRendererPrefabs("Assets/Resources/Art/Items", "Assets/Prefabs/Art/Items", 1.1f);
            CreateSpriteRendererPrefabs("Assets/Resources/Art/Map", "Assets/Prefabs/Art/Map", 1.1f);
            CreateSpriteRendererPrefabs("Assets/Resources/Art/Town", "Assets/Prefabs/Art/Town", 1.1f);
            CreateSpriteRendererPrefabs("Assets/Resources/Art/Quests", "Assets/Prefabs/Art/Quests", 1.1f);
            CreateSpriteRendererPrefabs("Assets/Resources/Art/FX", "Assets/Prefabs/Art/FX", 1.1f);
            CreateUiPrefabs("Assets/Resources/Art/UI/Icons", "Assets/Prefabs/UI/Generated/Icons");
            CreateUiPrefabs("Assets/Resources/Art/UI/Panels", "Assets/Prefabs/UI/Generated/Panels");
            AssignEnemySprites();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Graph art prefabs and config sprite links rebuilt.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs/Art");
            EnsureFolder("Assets/Prefabs/Art/Characters");
            EnsureFolder("Assets/Prefabs/Art/Characters/Hero");
            EnsureFolder("Assets/Prefabs/Art/Characters/Enemies");
            EnsureFolder("Assets/Prefabs/Art/Items");
            EnsureFolder("Assets/Prefabs/Art/Map");
            EnsureFolder("Assets/Prefabs/Art/Town");
            EnsureFolder("Assets/Prefabs/Art/Quests");
            EnsureFolder("Assets/Prefabs/Art/FX");
            EnsureFolder("Assets/Prefabs/UI/Generated");
            EnsureFolder("Assets/Prefabs/UI/Generated/Icons");
            EnsureFolder("Assets/Prefabs/UI/Generated/Panels");
        }

        private static void CreateSpriteRendererPrefabs(string sourceFolder, string prefabFolder, float scale)
        {
            foreach (var path in Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                var assetPath = Normalize(path);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(path);
                var go = new GameObject(name);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
                go.transform.localScale = Vector3.one * scale;
                PrefabUtility.SaveAsPrefabAsset(go, $"{prefabFolder}/{name}.prefab");
                Object.DestroyImmediate(go);
            }
        }

        private static void CreateUiPrefabs(string sourceFolder, string prefabFolder)
        {
            foreach (var path in Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                var assetPath = Normalize(path);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(path);
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(96, 96);
                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                PrefabUtility.SaveAsPrefabAsset(go, $"{prefabFolder}/{name}.prefab");
                Object.DestroyImmediate(go);
            }
        }

        private static void AssignEnemySprites()
        {
            AssignEnemySprite("slime_green", "Assets/Resources/Art/Characters/Enemies/enemy_slime_green_idle.png");
            AssignEnemySprite("cave_bat", "Assets/Resources/Art/Characters/Enemies/enemy_cave_bat_idle.png");
            AssignEnemySprite("stone_golem", "Assets/Resources/Art/Characters/Enemies/enemy_stone_golem_idle.png");
        }

        private static void AssignEnemySprite(string enemyId, string spritePath)
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>($"Assets/ScriptableObjects/Enemies/{enemyId}.asset");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (config == null || sprite == null)
            {
                return;
            }

            config.Sprite = sprite;
            EditorUtility.SetDirty(config);
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

        private static string Normalize(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}
