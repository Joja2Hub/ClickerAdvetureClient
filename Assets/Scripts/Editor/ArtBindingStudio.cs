using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure.EditorTools
{
    public sealed class ArtBindingStudio : EditorWindow
    {
        private const string DatabasePath = "Assets/Resources/Art/ArtBindings.asset";
        private const float TileSize = 92f;

        private readonly List<Sprite> sprites = new();
        private readonly List<BindingTarget> targets = new();
        private readonly List<string> runtimeKeys = new();

        private Vector2 spriteScroll;
        private Vector2 targetScroll;
        private string spriteSearch = "";
        private string targetSearch = "";
        private string keySearch = "";
        private bool sourceSheetsOnly;
        private bool showWorkflow = true;
        private Sprite selectedSprite;
        private BindingTarget selectedTarget;
        private string selectedRuntimeKey;
        private ArtBindingDatabase database;

        private sealed class BindingTarget
        {
            public UnityEngine.Object Owner;
            public string PropertyPath;
            public string Label;
            public Sprite Current;
        }

        [MenuItem("RPG Quest/Art/Art Binding Studio")]
        public static void Open()
        {
            var window = GetWindow<ArtBindingStudio>("Art Binding Studio");
            window.minSize = new Vector2(980f, 600f);
            window.RefreshAll();
        }

        private void OnEnable()
        {
            RefreshAll();
            Selection.selectionChanged += RefreshTargets;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= RefreshTargets;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawWorkflow();
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSpriteBrowser();
                DrawAssignmentPanel();
            }
        }

        private void DrawWorkflow()
        {
            showWorkflow = EditorGUILayout.Foldout(showWorkflow, "Workflow", true);
            if (!showWorkflow)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "1. Select a texture sheet in Project and click Prepare selected sheet.\n" +
                "2. Slice it in Sprite Editor and apply the changes.\n" +
                "3. Select the required sprite on the left.\n" +
                "4. Assign it to a prefab, scene object, or config in the center.\n" +
                "5. For UI created from code, save a runtime key override on the right.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    RefreshAll();
                }

                if (GUILayout.Button("Prepare selected sheet", EditorStyles.toolbarButton, GUILayout.Width(145)))
                {
                    PrepareSelectedSheet();
                }

                if (GUILayout.Button("Open Sprite Editor", EditorStyles.toolbarButton, GUILayout.Width(125)))
                {
                    OpenSpriteEditor();
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a sprite, then assign it to a target or runtime key.");
            }
        }

        private void DrawSpriteBrowser()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.48f)))
            {
                EditorGUILayout.LabelField("1. Sprites", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    spriteSearch = EditorGUILayout.TextField(spriteSearch, EditorStyles.toolbarSearchField);
                    sourceSheetsOnly = GUILayout.Toggle(
                        sourceSheetsOnly,
                        "SourceSheets only",
                        EditorStyles.miniButton,
                        GUILayout.Width(125));
                }

                var filtered = sprites
                    .Where(MatchesSpriteFilter)
                    .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                EditorGUILayout.LabelField($"{filtered.Count} sprites", EditorStyles.miniLabel);
                spriteScroll = EditorGUILayout.BeginScrollView(spriteScroll);
                var columns = Mathf.Max(1, Mathf.FloorToInt((position.width * 0.48f - 24f) / TileSize));

                for (var index = 0; index < filtered.Count; index += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (var column = 0; column < columns; column++)
                        {
                            var itemIndex = index + column;
                            if (itemIndex >= filtered.Count)
                            {
                                GUILayout.Space(TileSize);
                                continue;
                            }

                            DrawSpriteTile(filtered[itemIndex]);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSpriteTile(Sprite sprite)
        {
            var selected = selectedSprite == sprite;
            var previous = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.45f, 0.8f, 1f);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(TileSize - 4f)))
            {
                var preview = AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite);
                if (GUILayout.Button(preview, GUIStyle.none, GUILayout.Width(72), GUILayout.Height(64)))
                {
                    selectedSprite = sprite;
                    Repaint();
                }

                var label = sprite.name.Length > 15 ? sprite.name[..14] + "…" : sprite.name;
                if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(72)))
                {
                    selectedSprite = sprite;
                    Repaint();
                }
            }

            GUI.backgroundColor = previous;
        }

        private void DrawAssignmentPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                DrawSelectedSprite();
                EditorGUILayout.Space(8);
                DrawDirectTargets();
                EditorGUILayout.Space(8);
                DrawRuntimeBindings();
            }
        }

        private void DrawSelectedSprite()
        {
            EditorGUILayout.LabelField("Selected graphic", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var preview = selectedSprite != null
                    ? AssetPreview.GetAssetPreview(selectedSprite) ?? AssetPreview.GetMiniThumbnail(selectedSprite)
                    : null;
                GUILayout.Label(preview, GUILayout.Width(96), GUILayout.Height(96));
                using (new EditorGUILayout.VerticalScope())
                {
                    selectedSprite = (Sprite)EditorGUILayout.ObjectField(
                        selectedSprite,
                        typeof(Sprite),
                        false);
                    if (selectedSprite != null)
                    {
                        EditorGUILayout.LabelField(selectedSprite.name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(
                            AssetDatabase.GetAssetPath(selectedSprite),
                            EditorStyles.wordWrappedMiniLabel);
                        if (GUILayout.Button("Ping sprite", GUILayout.Width(100)))
                        {
                            EditorGUIUtility.PingObject(selectedSprite);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Choose a sprite in the grid.", MessageType.Info);
                    }
                }
            }
        }

        private void DrawDirectTargets()
        {
            EditorGUILayout.LabelField("2. Selected object / prefab / config", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select GameObjects, prefab assets, or ScriptableObject configs in Unity. " +
                "All Sprite fields, Image components, and SpriteRenderers will appear below.",
                MessageType.None);

            targetSearch = EditorGUILayout.TextField(targetSearch, EditorStyles.toolbarSearchField);
            var filtered = targets
                .Where(item => string.IsNullOrWhiteSpace(targetSearch)
                    || item.Label.IndexOf(targetSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            targetScroll = EditorGUILayout.BeginScrollView(targetScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(220));
            foreach (var target in filtered)
            {
                var selected = ReferenceEquals(selectedTarget, target);
                using (new EditorGUILayout.HorizontalScope(selected ? EditorStyles.helpBox : GUIStyle.none))
                {
                    if (GUILayout.Toggle(selected, "", GUILayout.Width(18)) && !selected)
                    {
                        selectedTarget = target;
                    }

                    GUILayout.Label(AssetPreview.GetMiniThumbnail(target.Current), GUILayout.Width(24), GUILayout.Height(24));
                    if (GUILayout.Button(target.Label, EditorStyles.label))
                    {
                        selectedTarget = target;
                        EditorGUIUtility.PingObject(target.Owner);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(selectedSprite == null || selectedTarget == null))
            {
                if (GUILayout.Button("Assign sprite to selected target", GUILayout.Height(30)))
                {
                    AssignDirect();
                }
            }
        }

        private void DrawRuntimeBindings()
        {
            EditorGUILayout.LabelField("3. Runtime UI binding", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this for graphics loaded through ArtLibrary. It changes login, map, battle, " +
                "quest board, HUD, and other UI created from code.",
                MessageType.None);

            keySearch = EditorGUILayout.TextField(keySearch, EditorStyles.toolbarSearchField);
            var keys = runtimeKeys
                .Where(key => string.IsNullOrWhiteSpace(keySearch)
                    || key.IndexOf(keySearch, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            var currentIndex = Mathf.Max(0, Array.IndexOf(keys, selectedRuntimeKey));
            if (keys.Length > 0)
            {
                currentIndex = EditorGUILayout.Popup("ArtLibrary key", currentIndex, keys);
                selectedRuntimeKey = keys[currentIndex];
            }
            else
            {
                EditorGUILayout.HelpBox("No matching runtime keys.", MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(selectedRuntimeKey))
            {
                var current = GetOverride(selectedRuntimeKey);
                EditorGUILayout.ObjectField("Current override", current, typeof(Sprite), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(selectedSprite == null))
                    {
                        if (GUILayout.Button("Save runtime override", GUILayout.Height(28)))
                        {
                            SaveRuntimeOverride();
                        }
                    }

                    using (new EditorGUI.DisabledScope(current == null))
                    {
                        if (GUILayout.Button("Clear override", GUILayout.Height(28)))
                        {
                            ClearRuntimeOverride();
                        }
                    }
                }
            }
        }

        private bool MatchesSpriteFilter(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(sprite);
            if (sourceSheetsOnly && !path.StartsWith("Assets/Art/SourceSheets/", StringComparison.Ordinal))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(spriteSearch)
                || sprite.name.IndexOf(spriteSearch, StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf(spriteSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RefreshAll()
        {
            LoadDatabase();
            RefreshSprites();
            RefreshRuntimeKeys();
            RefreshTargets();
            Repaint();
        }

        private void RefreshSprites()
        {
            sprites.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[]
                     {
                         "Assets/Art/SourceSheets",
                         "Assets/Resources/Art"
                     }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            }

            sprites.RemoveAll(sprite => sprite == null);
            var unique = sprites.Distinct().ToArray();
            sprites.Clear();
            sprites.AddRange(unique);
        }

        private void RefreshRuntimeKeys()
        {
            runtimeKeys.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources/Art" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/Resources/", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                {
                    var resourcePath = path["Assets/Resources/".Length..];
                    resourcePath = Path.ChangeExtension(resourcePath, null)?.Replace("\\", "/");
                    if (!string.IsNullOrEmpty(resourcePath))
                    {
                        runtimeKeys.Add(resourcePath);
                    }
                }
            }

            AddPrefabRuntimeKeys(
                "Assets/Prefabs/UI/Generated/Icons",
                "Art/UI/Icons");
            AddPrefabRuntimeKeys(
                "Assets/Prefabs/UI/Generated/Panels",
                "Art/UI/Panels");
            AddPrefabRuntimeKeys(
                "Assets/Prefabs/UI/Generated/Rewards",
                "Art/Rewards");
            AddPrefabRuntimeKeys(
                "Assets/Prefabs/Art/Characters/Hero",
                "Art/Characters/Hero");
            AddPrefabRuntimeKeys(
                "Assets/Prefabs/Art/Characters/Enemies",
                "Art/Characters/Enemies");
            AddPrefabRuntimeKeys("Assets/Prefabs/Art/Map", "Art/Map");
            AddPrefabRuntimeKeys("Assets/Prefabs/Art/Town", "Art/Town");
            AddPrefabRuntimeKeys("Assets/Prefabs/Art/Quests", "Art/Quests");
            runtimeKeys.Add("Art/Rewards/reward_gold");
            runtimeKeys.Add("Art/Rewards/reward_xp");

            if (database != null)
            {
                runtimeKeys.AddRange(database.Entries.Select(entry => entry.Key));
            }

            runtimeKeys.Sort(StringComparer.OrdinalIgnoreCase);
            var unique = runtimeKeys.Distinct(StringComparer.Ordinal).ToArray();
            runtimeKeys.Clear();
            runtimeKeys.AddRange(unique);
            if (string.IsNullOrEmpty(selectedRuntimeKey) || !runtimeKeys.Contains(selectedRuntimeKey))
            {
                selectedRuntimeKey = runtimeKeys.FirstOrDefault();
            }
        }

        private void AddPrefabRuntimeKeys(string prefabFolder, string runtimeFolder)
        {
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    runtimeKeys.Add($"{runtimeFolder}/{name}");
                }
            }
        }

        private void RefreshTargets()
        {
            targets.Clear();
            foreach (var selected in Selection.objects)
            {
                switch (selected)
                {
                    case GameObject gameObject:
                        AddGameObjectTargets(gameObject);
                        break;
                    case Component component:
                        AddGameObjectTargets(component.gameObject);
                        break;
                    case ScriptableObject scriptableObject:
                        AddSerializedSpriteTargets(scriptableObject, scriptableObject.name);
                        break;
                }
            }

            selectedTarget = targets.FirstOrDefault();
            Repaint();
        }

        private void AddGameObjectTargets(GameObject root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                targets.Add(new BindingTarget
                {
                    Owner = image,
                    PropertyPath = "m_Sprite",
                    Label = $"{root.name} / {GetHierarchyPath(image.transform, root.transform)} / Image",
                    Current = image.sprite
                });
            }

            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                targets.Add(new BindingTarget
                {
                    Owner = renderer,
                    PropertyPath = "m_Sprite",
                    Label = $"{root.name} / {GetHierarchyPath(renderer.transform, root.transform)} / SpriteRenderer",
                    Current = renderer.sprite
                });
            }
        }

        private void AddSerializedSpriteTargets(UnityEngine.Object owner, string prefix)
        {
            var serialized = new SerializedObject(owner);
            var iterator = serialized.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                var value = iterator.objectReferenceValue;
                var fieldType = GetFieldType(owner.GetType(), iterator.propertyPath);
                if (fieldType != typeof(Sprite) && value is not Sprite)
                {
                    continue;
                }

                targets.Add(new BindingTarget
                {
                    Owner = owner,
                    PropertyPath = iterator.propertyPath,
                    Label = $"{prefix} / {iterator.displayName}",
                    Current = value as Sprite
                });
            }
        }

        private void AssignDirect()
        {
            if (selectedTarget?.Owner == null || selectedSprite == null)
            {
                return;
            }

            Undo.RecordObject(selectedTarget.Owner, "Assign art sprite");
            var serialized = new SerializedObject(selectedTarget.Owner);
            var property = serialized.FindProperty(selectedTarget.PropertyPath);
            if (property == null)
            {
                Debug.LogError($"Sprite property not found: {selectedTarget.PropertyPath}");
                return;
            }

            property.objectReferenceValue = selectedSprite;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedTarget.Owner);

            if (selectedTarget.Owner is Component component)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                var root = component.transform.root.gameObject;
                if (PrefabUtility.IsPartOfPrefabAsset(root))
                {
                    PrefabUtility.SavePrefabAsset(root);
                }
            }

            AssetDatabase.SaveAssets();
            selectedTarget.Current = selectedSprite;
            Debug.Log($"Assigned {selectedSprite.name} to {selectedTarget.Label}.");
        }

        private void SaveRuntimeOverride()
        {
            if (database == null || selectedSprite == null || string.IsNullOrEmpty(selectedRuntimeKey))
            {
                return;
            }

            Undo.RecordObject(database, "Set runtime art override");
            database.Set(selectedRuntimeKey, selectedSprite);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            ArtLibrary.ClearCache();
            Debug.Log($"Runtime art override saved: {selectedRuntimeKey} -> {selectedSprite.name}");
        }

        private void ClearRuntimeOverride()
        {
            if (database == null || string.IsNullOrEmpty(selectedRuntimeKey))
            {
                return;
            }

            Undo.RecordObject(database, "Clear runtime art override");
            database.Remove(selectedRuntimeKey);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            ArtLibrary.ClearCache();
        }

        private Sprite GetOverride(string key)
        {
            return database != null && database.TryGet(key, out var sprite) ? sprite : null;
        }

        private void LoadDatabase()
        {
            database = AssetDatabase.LoadAssetAtPath<ArtBindingDatabase>(DatabasePath);
            if (database != null)
            {
                return;
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Art");
            database = CreateInstance<ArtBindingDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
        }

        private static void PrepareSelectedSheet()
        {
            var texture = Selection.activeObject as Texture2D;
            if (texture == null)
            {
                EditorUtility.DisplayDialog(
                    "Art Binding Studio",
                    "Select a texture from Assets/Art/SourceSheets first.",
                    "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(texture);
            if (!path.StartsWith("Assets/Art/SourceSheets/", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog(
                    "Art Binding Studio",
                    "The selected texture is not in Assets/Art/SourceSheets.",
                    "OK");
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            OpenSpriteEditor();
        }

        private static void OpenSpriteEditor()
        {
            if (Selection.activeObject is not Texture2D)
            {
                EditorUtility.DisplayDialog(
                    "Art Binding Studio",
                    "Select a source sheet texture first.",
                    "OK");
                return;
            }

            if (!EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor"))
            {
                Debug.LogWarning("Sprite Editor could not be opened. Open Window > 2D > Sprite Editor manually.");
            }
        }

        private static string GetHierarchyPath(Transform target, Transform root)
        {
            if (target == root)
            {
                return target.name;
            }

            var names = new Stack<string>();
            var current = target;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                {
                    break;
                }
                current = current.parent;
            }
            return string.Join("/", names);
        }

        private static Type GetFieldType(Type type, string propertyPath)
        {
            var fieldName = propertyPath.Split('.')[0];
            while (type != null)
            {
                var field = type.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.FieldType;
                }
                type = type.BaseType;
            }
            return null;
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
