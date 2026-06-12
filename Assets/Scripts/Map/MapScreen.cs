using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class MapScreen : MonoBehaviour
    {
        [SerializeField] private MapNodeConfig[] nodes = System.Array.Empty<MapNodeConfig>();

        private Text header;
        private Text statusText;
        private RectTransform mapSurface;
        private List<MapNodeState> nodeStates = new();
        private bool isApplyingTestReward;

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
            if (mapBackground != null)
            {
                RuntimeUi.SpriteBackground(canvas.transform, "MapBackground", mapBackground, new Color(0.11f, 0.17f, 0.18f));
            }
            else
            {
                RuntimeUi.AdventureBackground(canvas.transform, new Color(0.28f, 0.52f, 0.65f), new Color(0.14f, 0.42f, 0.31f), new Color(0.11f, 0.17f, 0.18f));
            }

            var root = RuntimeUi.SafeAreaRoot(canvas.transform);

            var topBar = RuntimeUi.SpriteCard(root, "TopBar", ArtLibrary.Panel("ui_panel_dark_wood"), new Color(0.15f, 0.12f, 0.10f, 0.92f));
            RuntimeUi.SetRect(topBar, new Vector2(0.5f, 0.95f), new Vector2(980, 86), Vector2.zero);

            header = RuntimeUi.Text(topBar, "Header", "", 28, Color.white, TextAnchor.MiddleLeft);
            header.rectTransform.offsetMin = new Vector2(28, 0);
            header.rectTransform.offsetMax = new Vector2(-24, 0);
            RefreshHeader();

            var title = RuntimeUi.Text(root, "MapTitle", "Карта приключений", 46, new Color(0.30f, 0.18f, 0.10f));
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(900, 78), Vector2.zero);

            mapSurface = new GameObject("MapSurface", typeof(RectTransform)).GetComponent<RectTransform>();
            mapSurface.SetParent(root, false);
            RuntimeUi.SetRect(mapSurface, new Vector2(0.5f, 0.51f), new Vector2(980, 1120), Vector2.zero);

            RuntimeUi.PathSegment(mapSurface, "Path_Forest_Town", new Vector2(-20, 145), 520, -20);
            RuntimeUi.PathSegment(mapSurface, "Path_Town_Cave", new Vector2(125, -115), 420, -55);

            nodeStates = ServiceRegistry.Map.BuildNodeStates(nodes, ServiceRegistry.GameState.Current).ToList();
            foreach (var state in nodeStates)
            {
                CreateMapNode(mapSurface, state);
            }

            statusText = RuntimeUi.Text(root, "Status", "", 25, new Color(0.28f, 0.16f, 0.08f));
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.15f), new Vector2(900, 68), Vector2.zero);

            CreateBottomButton(root, "GuildButton", "Гильдия", ArtLibrary.Icon("ui_icon_guild"), new Vector2(0.24f, 0.07f), OpenTownFromFirstTownNode);
            CreateBottomButton(root, "InventoryButton", "Инвентарь", ArtLibrary.Icon("ui_icon_bag"), new Vector2(0.5f, 0.07f), () => statusText.text = "Инвентарь откроется в следующем этапе.");
            CreateBottomButton(root, "SettingsButton", "Настройки", ArtLibrary.Icon("ui_icon_settings"), new Vector2(0.76f, 0.07f), () => statusText.text = "Настройки откроются в следующем этапе.");
        }

        private void RefreshHeader()
        {
            var state = ServiceRegistry.GameState.Current;
            header.text = $"Уровень {state.Level}   XP {state.Xp}   Золото {state.Gold}";
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
            RuntimeUi.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(220, 230), state.Config.MapPosition);

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
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(160, 160), Vector2.zero);

            var button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.interactable = state.IsUnlocked;
            button.onClick.AddListener(() => OnNodeClicked(state));

            if (!state.IsUnlocked)
            {
                var lockIcon = RuntimeUi.SpriteImage(root, "Lock", ArtLibrary.Icon("ui_icon_lock"));
                RuntimeUi.SetRect(lockIcon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(64, 64), Vector2.zero);
            }

            var labelText = state.IsUnlocked
                ? state.Config.DisplayName
                : $"{state.Config.DisplayName}\nур. {state.Config.RequiredLevel}";
            var label = RuntimeUi.Text(root, "Label", labelText, 24, new Color(0.23f, 0.13f, 0.07f));
            label.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(label.rectTransform, new Vector2(0.5f, 0.13f), new Vector2(260, 70), Vector2.zero);
        }

        private void CreateBottomButton(Transform parent, string name, string label, Sprite iconSprite, Vector2 anchor, System.Action onClick)
        {
            var button = RuntimeUi.Button(parent, name, label, new Color(0.30f, 0.25f, 0.20f, 0.92f), onClick);
            RuntimeUi.SetRect(button.GetComponent<RectTransform>(), anchor, new Vector2(280, 74), Vector2.zero);

            if (iconSprite == null)
            {
                return;
            }

            var icon = RuntimeUi.SpriteImage(button.transform, "Icon", iconSprite);
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.17f, 0.5f), new Vector2(42, 42), Vector2.zero);
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
            RefreshHeader();
            statusText.text = "Тестовый прогресс добавлен.";
            isApplyingTestReward = false;
        }
    }
}
