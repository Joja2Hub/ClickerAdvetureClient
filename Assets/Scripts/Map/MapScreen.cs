using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class MapScreen : MonoBehaviour
    {
        [SerializeField] private MapNodeConfig[] nodes = System.Array.Empty<MapNodeConfig>();
        private Text header;
        private RectTransform mapSurface;

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
            RuntimeUi.AdventureBackground(canvas.transform, new Color(0.28f, 0.52f, 0.65f), new Color(0.14f, 0.42f, 0.31f), new Color(0.11f, 0.17f, 0.18f));

            var topBar = RuntimeUi.Card(canvas.transform, "TopBar", new Color(0.08f, 0.12f, 0.14f, 0.86f));
            RuntimeUi.SetRect(topBar, new Vector2(0.5f, 0.94f), new Vector2(980, 88), Vector2.zero);

            header = RuntimeUi.Text(topBar, "Header", "", 30, Color.white, TextAnchor.MiddleLeft);
            header.rectTransform.offsetMin = new Vector2(28, 0);
            header.rectTransform.offsetMax = new Vector2(-24, 0);
            RefreshHeader();

            var title = RuntimeUi.Text(canvas.transform, "MapTitle", "Карта приключений", 52, new Color(0.98f, 0.9f, 0.65f));
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(900, 84), Vector2.zero);

            mapSurface = RuntimeUi.Card(canvas.transform, "MapSurface", new Color(0.78f, 0.65f, 0.43f, 0.94f));
            RuntimeUi.SetRect(mapSurface, new Vector2(0.5f, 0.48f), new Vector2(940, 1040), Vector2.zero);

            RuntimeUi.PathSegment(mapSurface, "Path_Forest_Town", new Vector2(-20, 145), 520, -20);
            RuntimeUi.PathSegment(mapSurface, "Path_Town_Cave", new Vector2(125, -115), 420, -55);

            var states = ServiceRegistry.Map.BuildNodeStates(nodes, ServiceRegistry.GameState.Current);
            foreach (var state in states)
            {
                CreateNodeButton(mapSurface, state);
            }

            var quests = RuntimeUi.Button(canvas.transform, "QuestButton", "Гильдия", new Color(0.42f, 0.36f, 0.72f), OpenTownFromFirstTownNode);
            RuntimeUi.SetRect(quests.GetComponent<RectTransform>(), new Vector2(0.24f, 0.08f), new Vector2(300, 76), Vector2.zero);

            var inventory = RuntimeUi.Button(canvas.transform, "InventoryButton", "Инвентарь", new Color(0.35f, 0.47f, 0.55f), () => Debug.Log("Inventory panel placeholder"));
            RuntimeUi.SetRect(inventory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), new Vector2(300, 76), Vector2.zero);

            var settings = RuntimeUi.Button(canvas.transform, "SettingsButton", "Настройки", new Color(0.45f, 0.45f, 0.45f), () => Debug.Log("Settings panel placeholder"));
            RuntimeUi.SetRect(settings.GetComponent<RectTransform>(), new Vector2(0.76f, 0.08f), new Vector2(300, 76), Vector2.zero);
        }

        private void RefreshHeader()
        {
            var state = ServiceRegistry.GameState.Current;
            header.text = $"Уровень {state.Level}   XP {state.Xp}   Золото {state.Gold}";
        }

        private void CreateNodeButton(Transform parent, MapNodeState state)
        {
            var color = state.IsUnlocked
                ? state.Config.Type == MapNodeType.Dungeon ? new Color(0.55f, 0.20f, 0.18f) : new Color(0.18f, 0.49f, 0.36f)
                : new Color(0.31f, 0.29f, 0.26f);
            var typeLabel = state.Config.Type == MapNodeType.Dungeon ? "Бой" : "Город";
            var label = state.IsUnlocked
                ? $"{state.Config.DisplayName}\n{typeLabel}"
                : $"{state.Config.DisplayName}\nОткроется на ур. {state.Config.RequiredLevel}";

            var button = RuntimeUi.Button(parent, $"Node_{state.Config.NodeId}", label, color, () => OnNodeClicked(state));
            var position = state.Config.MapPosition;
            RuntimeUi.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(350, 120), position);
            button.interactable = state.IsUnlocked;

            var markerColor = state.IsUnlocked ? RuntimeUi.Gold : new Color(0.45f, 0.45f, 0.45f);
            var marker = RuntimeUi.Circle(parent, $"Marker_{state.Config.NodeId}", markerColor);
            RuntimeUi.SetRect(marker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(76, 76), position + new Vector2(-175, 0));
            marker.raycastTarget = false;
        }

        private void OnNodeClicked(MapNodeState state)
        {
            if (!state.IsUnlocked)
            {
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
    }
}
