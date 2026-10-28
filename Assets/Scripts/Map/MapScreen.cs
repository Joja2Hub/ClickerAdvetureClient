using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class MapScreen : MonoBehaviour
    {
        [SerializeField] private MapNodeConfig[] nodes = System.Array.Empty<MapNodeConfig>();

        private Text statusText;
        private RectTransform mapSurface;
        private List<MapNodeState> nodeStates = new();
        private bool isApplyingTestReward;
        private const float MapSurfaceSize = 2180f;
        private const float MapNodePositionScale = 1.62f;
        private const float MapNodeRootSize = 238f;
        private const float MapNodeIconSize = 158f;

        private async void Start()
        {
            await ServiceRegistry.InitializeAsync();
            if (!ServiceRegistry.Auth.IsSignedIn)
            {
                await ServiceRegistry.Auth.DevLoginAsync();
            }

            var childId = ServiceRegistry.Auth.CurrentUser.ChildId;
            await ServiceRegistry.PlayerProfiles.LoadChildProfileAsync(childId);
            await ServiceRegistry.GameState.LoadAsync(childId);
            Build();
        }

        private void Build()
        {
            var canvas = RuntimeUi.CreateCanvas();
            var mapBackground = ArtLibrary.Background("bg_world_map_parchment");
            RuntimeUi.Panel(canvas.transform, "ScreenBackground", new Color(0.11f, 0.17f, 0.18f));

            var root = RuntimeUi.SafeAreaRoot(canvas.transform);

            var mapViewport = RuntimeUi.Panel(root, "MapViewport", new Color(0.05f, 0.07f, 0.06f, 0.96f));
            RuntimeUi.Stretch(mapViewport);
            mapViewport.gameObject.AddComponent<RectMask2D>();
            var mapScroll = mapViewport.gameObject.AddComponent<ScrollRect>();
            mapScroll.horizontal = true;
            mapScroll.vertical = true;
            mapScroll.inertia = true;
            mapScroll.decelerationRate = 0.08f;
            mapScroll.movementType = ScrollRect.MovementType.Clamped;
            mapScroll.scrollSensitivity = 0f;
            mapScroll.viewport = mapViewport;

            mapSurface = new GameObject("MapSurface", typeof(RectTransform)).GetComponent<RectTransform>();
            mapSurface.SetParent(mapViewport, false);
            RuntimeUi.SetRect(mapSurface, new Vector2(0.5f, 0.5f), new Vector2(MapSurfaceSize, MapSurfaceSize), Vector2.zero);
            mapScroll.content = mapSurface;

            var surfaceBackground = RuntimeUi.SpriteImage(
                mapSurface,
                "MapBackground",
                mapBackground,
                true);
            RuntimeUi.Stretch(surfaceBackground.rectTransform);
            surfaceBackground.color = mapBackground != null
                ? Color.white
                : new Color(0.28f, 0.42f, 0.31f);
            surfaceBackground.transform.SetAsFirstSibling();

            var mapVignette = RuntimeUi.Panel(mapViewport, "MapVignette", new Color(0f, 0f, 0f, 0.10f));
            mapVignette.SetAsLastSibling();
            mapVignette.GetComponent<Image>().raycastTarget = false;

            var mapZoom = mapViewport.gameObject.AddComponent<MapPanZoom>();
            mapZoom.Initialize(mapSurface, mapScroll);
            StartCoroutine(CenterMapNextFrame(mapScroll));

            RuntimeUi.PathSegment(mapSurface, "Path_Forest_Town", new Vector2(-30, 205), 650, -20);
            RuntimeUi.PathSegment(mapSurface, "Path_Town_Cave", new Vector2(175, -160), 540, -55);
            RuntimeUi.PathSegment(mapSurface, "Path_Cave_Swamp", new Vector2(-135, -405), 520, 18);
            RuntimeUi.PathSegment(mapSurface, "Path_Town_Mountain", new Vector2(440, 195), 500, 38);
            RuntimeUi.PathSegment(mapSurface, "Path_Mountain_Tower", new Vector2(520, 485), 420, 78);

            nodeStates = ServiceRegistry.Map.BuildNodeStates(nodes, ServiceRegistry.GameState.Current).ToList();
            foreach (var state in nodeStates)
            {
                CreateMapNode(mapSurface, state);
            }
            CreateFutureDungeonNode(mapSurface, "Туманные топи", new Vector2(-520, -500), 5);
            CreateFutureDungeonNode(mapSurface, "Ветреные пики", new Vector2(565, 380), 7);
            CreateFutureDungeonNode(mapSurface, "Башня теней", new Vector2(500, 650), 9);
            CreateFutureDungeonNode(mapSurface, "Древний замок", new Vector2(-510, 570), 12);

            ResourceHud.Create(root);

            var title = RuntimeUi.Text(root, "MapTitle", "Карта приключений", 44, new Color(0.30f, 0.18f, 0.10f));
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(900, 74), Vector2.zero);

            CreateZoomButton(root, "ZoomIn", "+", new Vector2(0.86f, 0.70f), mapZoom.ZoomIn);
            CreateZoomButton(root, "ZoomOut", "-", new Vector2(0.86f, 0.62f), mapZoom.ZoomOut);

            statusText = RuntimeUi.Text(root, "Status", "", 25, new Color(0.28f, 0.16f, 0.08f));
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.145f), new Vector2(900, 68), Vector2.zero);

            CreateBottomButton(root, "GuildButton", "Гильдия", ArtLibrary.Icon("ui_icon_guild"), new Vector2(0.33f, 0.07f), OpenTownFromFirstTownNode);
            CreateBottomButton(root, "InventoryButton", "Инвентарь", ArtLibrary.Icon("ui_icon_bag"), new Vector2(0.5f, 0.07f), () => statusText.text = "Инвентарь откроется в следующем этапе.");
            CreateBottomButton(root, "SettingsButton", "Настройки", ArtLibrary.Icon("ui_icon_settings"), new Vector2(0.67f, 0.07f), () => statusText.text = "Настройки откроются в следующем этапе.");
        }

        private void Update()
        {
            if (!DesktopTestMode.IsEnabled || isApplyingTestReward)
            {
                return;
            }

            for (var i = 0; i < Mathf.Min(9, nodeStates.Count); i++)
            {
                var alphaKey = (KeyCode)((int)KeyCode.Alpha1 + i);
                var keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);
                if (DesktopTestMode.Pressed(alphaKey) || DesktopTestMode.Pressed(keypadKey))
                {
                    OnNodeClicked(nodeStates[i]);
                    return;
                }
            }

            if (DesktopTestMode.Pressed(KeyCode.G))
            {
                OpenTownFromFirstTownNode();
            }

            if (DesktopTestMode.Pressed(KeyCode.F5))
            {
                _ = AddTestProgressAsync();
            }

            if (DesktopTestMode.Pressed(KeyCode.Escape))
            {
                SceneLoader.LoadLogin();
            }
        }

        private void CreateMapNode(Transform parent, MapNodeState state)
        {
            var root = new GameObject($"Node_{state.Config.NodeId}", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            RuntimeUi.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(MapNodeRootSize, MapNodeRootSize), state.Config.MapPosition * MapNodePositionScale);

            var sprite = ArtLibrary.Map(state.Config.NodeId switch
            {
                "home_village" => "map_node_home_village",
                "sunny_forest" => "map_node_sunny_forest",
                "crystal_cave" => "map_node_crystal_cave",
                _ => state.IsUnlocked ? "map_star_marker" : "map_node_locked"
            });

            var icon = RuntimeUi.SpriteImage(root, "Icon", sprite);
            icon.color = state.IsUnlocked ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.75f);
            icon.raycastTarget = true;
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.5f, 0.61f), new Vector2(MapNodeIconSize, MapNodeIconSize), Vector2.zero);

            var button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.interactable = state.IsUnlocked;
            button.onClick.AddListener(() => OnNodeClicked(state));

            if (!state.IsUnlocked)
            {
                var lockIcon = RuntimeUi.SpriteImage(root, "Lock", ArtLibrary.Icon("ui_icon_lock"));
                RuntimeUi.SetRect(lockIcon.rectTransform, new Vector2(0.5f, 0.61f), new Vector2(62, 62), Vector2.zero);
            }

            var labelText = state.IsUnlocked
                ? state.Config.DisplayName
                : $"{state.Config.DisplayName}\nур. {state.Config.RequiredLevel}";
            var label = RuntimeUi.Text(root, "Label", labelText, 23, new Color(0.23f, 0.13f, 0.07f));
            label.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(label.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(280, 70), Vector2.zero);
        }

        private static void CreateFutureDungeonNode(Transform parent, string displayName, Vector2 position, int requiredLevel)
        {
            var root = new GameObject($"FutureDungeon_{requiredLevel}", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            RuntimeUi.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(MapNodeRootSize, MapNodeRootSize), position * 1.16f);

            var icon = RuntimeUi.SpriteImage(root, "Icon", ArtLibrary.Map("map_node_locked"));
            icon.color = new Color(0.55f, 0.55f, 0.55f, 0.78f);
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.5f, 0.61f), new Vector2(MapNodeIconSize, MapNodeIconSize), Vector2.zero);

            var lockIcon = RuntimeUi.SpriteImage(root, "Lock", ArtLibrary.Icon("ui_icon_lock"));
            RuntimeUi.SetRect(lockIcon.rectTransform, new Vector2(0.5f, 0.61f), new Vector2(62, 62), Vector2.zero);

            var label = RuntimeUi.Text(
                root,
                "Label",
                $"{displayName}\nур. {requiredLevel}",
                23,
                new Color(0.23f, 0.13f, 0.07f));
            label.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(label.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(290, 70), Vector2.zero);
        }

        private static IEnumerator CenterMapNextFrame(ScrollRect mapScroll)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (mapScroll == null)
            {
                yield break;
            }

            mapScroll.StopMovement();
            mapScroll.normalizedPosition = new Vector2(0.5f, 0.5f);
        }

        private static void CreateZoomButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            System.Action onClick)
        {
            var button = RuntimeUi.Button(
                parent,
                name,
                label,
                new Color(0.24f, 0.20f, 0.16f, 0.94f),
                onClick);
            RuntimeUi.SetRect(button.GetComponent<RectTransform>(), anchor, new Vector2(72, 72), Vector2.zero);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 42;
                text.resizeTextMaxSize = 42;
            }
        }

        private void CreateBottomButton(Transform parent, string name, string label, Sprite iconSprite, Vector2 anchor, System.Action onClick)
        {
            if (name == "InventoryButton")
            {
                return;
            }

            var button = RuntimeUi.Button(parent, name, label, UiTheme.Surface, onClick);
            RuntimeUi.SetRect(button.GetComponent<RectTransform>(), anchor, new Vector2(280, 74), Vector2.zero);

            if (iconSprite == null)
            {
                return;
            }

            var icon = RuntimeUi.SpriteImage(button.transform, "Icon", iconSprite);
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.17f, 0.5f), new Vector2(42, 42), Vector2.zero);
            var text = button.GetComponentInChildren<Text>();
            text.rectTransform.offsetMin = new Vector2(72, 4);
            text.resizeTextMaxSize = 26;
        }

        private void OnNodeClicked(MapNodeState state)
        {
            if (!state.IsUnlocked)
            {
                if (statusText != null)
                {
                    statusText.text = $"Локация откроется на уровне {state.Config.RequiredLevel}.";
                }

                return;
            }

            if (state.Config.Type == MapNodeType.Dungeon)
            {
                SceneLoader.LoadBattle(state.Config.Dungeon);
                return;
            }

            SceneLoader.LoadTown(state.Config.Town);
        }

        private void OpenTownFromFirstTownNode()
        {
            var town = nodes.FirstOrDefault(node => node != null && node.Town != null)?.Town;
            SceneLoader.LoadTown(town);
        }

        private async Task AddTestProgressAsync()
        {
            isApplyingTestReward = true;
            var childId = ServiceRegistry.Auth.CurrentUser.ChildId;
            await ServiceRegistry.GameState.AddBattleRewardAsync(childId, new BattleReward { Xp = 125, Gold = 25 });
            statusText.text = "Тестовый прогресс добавлен.";
            isApplyingTestReward = false;
        }
    }
}
