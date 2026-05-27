using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class TownScreen : MonoBehaviour
    {
        private RectTransform questList;
        private Text statusText;
        private QuestStatus? filter;
        private List<RealLifeQuest> visibleQuests = new();
        private bool isBusy;

        private async void Start()
        {
            await ServiceRegistry.InitializeAsync();
            if (!ServiceRegistry.Auth.IsSignedIn)
            {
                await ServiceRegistry.Auth.DevLoginAsync();
                await ServiceRegistry.GameState.LoadAsync(ServiceRegistry.Auth.CurrentUser.ChildId);
            }

            Build();
            await RefreshQuestsAsync();
        }

        private void Update()
        {
            if (!DesktopTestMode.IsEnabled || isBusy)
            {
                return;
            }

            if (DesktopTestMode.Pressed(KeyCode.Escape))
            {
                SceneLoader.LoadMap();
                return;
            }

            if (DesktopTestMode.Pressed(KeyCode.Alpha1))
            {
                SetFilter(null);
            }
            else if (DesktopTestMode.Pressed(KeyCode.Alpha2))
            {
                SetFilter(QuestStatus.Assigned);
            }
            else if (DesktopTestMode.Pressed(KeyCode.Alpha3))
            {
                SetFilter(QuestStatus.Submitted);
            }
            else if (DesktopTestMode.Pressed(KeyCode.Alpha4))
            {
                SetFilter(QuestStatus.Approved);
            }
            else if (DesktopTestMode.Pressed(KeyCode.Alpha5))
            {
                SetFilter(QuestStatus.Claimed);
            }

            if (DesktopTestMode.Pressed(KeyCode.R))
            {
                _ = RefreshQuestsAsync();
            }

            if (DesktopTestMode.PressedAny(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space))
            {
                var quest = visibleQuests.FirstOrDefault(q => q.Status is QuestStatus.Assigned or QuestStatus.Rejected or QuestStatus.Approved);
                if (quest != null)
                {
                    _ = OnQuestActionAsync(quest);
                }
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.CreateCanvas();
            RuntimeUi.AdventureBackground(canvas.transform, new Color(0.30f, 0.45f, 0.52f), new Color(0.32f, 0.38f, 0.28f), new Color(0.12f, 0.13f, 0.12f));

            var town = SelectionContext.SelectedTown;
            var title = RuntimeUi.Text(canvas.transform, "TownTitle", town != null ? town.DisplayName : "Город", 52, new Color(0.98f, 0.88f, 0.56f));
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(900, 80), Vector2.zero);

            CreateTownActions(canvas.transform);

            var guildPanel = RuntimeUi.Card(canvas.transform, "GuildPanel", new Color(0.13f, 0.18f, 0.18f, 0.88f));
            RuntimeUi.SetRect(guildPanel, new Vector2(0.5f, 0.51f), new Vector2(950, 1030), Vector2.zero);

            var subtitle = RuntimeUi.Text(guildPanel, "Subtitle", "Гильдия приключений", 38, Color.white);
            subtitle.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.94f), new Vector2(880, 70), Vector2.zero);

            CreateFilterButton(guildPanel, "Все", null, 0.12f);
            CreateFilterButton(guildPanel, "Активные", QuestStatus.Assigned, 0.30f);
            CreateFilterButton(guildPanel, "Проверка", QuestStatus.Submitted, 0.50f);
            CreateFilterButton(guildPanel, "Одобрено", QuestStatus.Approved, 0.70f);
            CreateFilterButton(guildPanel, "Получено", QuestStatus.Claimed, 0.88f);

            questList = new GameObject("QuestList", typeof(RectTransform)).GetComponent<RectTransform>();
            questList.SetParent(guildPanel, false);
            RuntimeUi.SetRect(questList, new Vector2(0.5f, 0.43f), new Vector2(900, 760), Vector2.zero);

            statusText = RuntimeUi.Text(canvas.transform, "Status", "", 28, new Color(1f, 0.88f, 0.45f));
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.14f), new Vector2(900, 90), Vector2.zero);

            var back = RuntimeUi.Button(canvas.transform, "BackButton", "Карта", new Color(0.35f, 0.42f, 0.48f), SceneLoader.LoadMap);
            RuntimeUi.SetRect(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(330, 76), Vector2.zero);
        }

        private async System.Threading.Tasks.Task RefreshQuestsAsync()
        {
            var childId = ServiceRegistry.Auth.CurrentUser.ChildId;
            var quests = await ServiceRegistry.Quests.LoadQuestsAsync(childId);
            RenderQuests(quests);
        }

        private void RenderQuests(List<RealLifeQuest> quests)
        {
            foreach (Transform child in questList)
            {
                Destroy(child.gameObject);
            }

            visibleQuests = filter.HasValue ? quests.Where(q => q.Status == filter.Value).ToList() : quests;
            for (var i = 0; i < visibleQuests.Count; i++)
            {
                CreateQuestRow(visibleQuests[i], i);
            }

            if (visibleQuests.Count == 0)
            {
                var empty = RuntimeUi.Text(questList, "Empty", "Здесь пока нет квестов.", 32, new Color(0.85f, 0.86f, 0.80f));
                RuntimeUi.SetRect(empty.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(800, 90), Vector2.zero);
            }
        }

        private void CreateQuestRow(RealLifeQuest quest, int index)
        {
            var row = RuntimeUi.Card(questList, $"Quest_{quest.Id}", new Color(0.22f, 0.30f, 0.28f, 0.96f));
            RuntimeUi.SetRect(row, new Vector2(0.5f, 1f), new Vector2(880, 190), new Vector2(0, -100 - index * 210));

            var title = RuntimeUi.Text(row.transform, "Title", $"{quest.Title}\n{quest.Description}", 28, Color.white, TextAnchor.MiddleLeft);
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.36f, 0.55f), new Vector2(520, 140), Vector2.zero);

            var reward = RuntimeUi.Text(row.transform, "Reward", $"+{quest.RewardXp} XP\n+{quest.RewardGold} золота", 24, new Color(1f, 0.86f, 0.45f));
            RuntimeUi.SetRect(reward.rectTransform, new Vector2(0.76f, 0.66f), new Vector2(240, 90), Vector2.zero);

            var status = RuntimeUi.Badge(row, "Status", StatusLabel(quest.Status), StatusColor(quest.Status), Color.white);
            RuntimeUi.SetRect(status, new Vector2(0.18f, 0.18f), new Vector2(220, 48), Vector2.zero);

            var buttonLabel = quest.Status switch
            {
                QuestStatus.Assigned or QuestStatus.Rejected => "На проверку",
                QuestStatus.Approved => "Забрать",
                QuestStatus.Submitted => "Ждём",
                QuestStatus.Claimed => "Получено",
                _ => "Недоступно"
            };

            var action = RuntimeUi.Button(row.transform, "Action", buttonLabel, new Color(0.36f, 0.45f, 0.70f), () => _ = OnQuestActionAsync(quest));
            RuntimeUi.SetRect(action.GetComponent<RectTransform>(), new Vector2(0.75f, 0.28f), new Vector2(230, 58), Vector2.zero);
            action.interactable = quest.Status is QuestStatus.Assigned or QuestStatus.Rejected or QuestStatus.Approved;
        }

        private void CreateTownActions(Transform parent)
        {
            var names = new[] { "Лавка", "Кузница", "Тренер", "Алхимия" };
            var colors = new[]
            {
                new Color(0.45f, 0.32f, 0.23f),
                new Color(0.43f, 0.43f, 0.47f),
                new Color(0.35f, 0.42f, 0.64f),
                new Color(0.22f, 0.50f, 0.44f)
            };

            for (var i = 0; i < names.Length; i++)
            {
                var local = i;
                var button = RuntimeUi.Button(parent, $"TownAction_{names[i]}", names[i], colors[i], () => statusText.text = $"{names[local]} откроется в следующем этапе MVP.");
                RuntimeUi.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.18f + i * 0.21f, 0.84f), new Vector2(190, 66), Vector2.zero);
            }
        }

        private void CreateFilterButton(Transform parent, string label, QuestStatus? status, float x)
        {
            var button = RuntimeUi.Button(parent, $"Filter_{label}", label, new Color(0.25f, 0.34f, 0.38f), () =>
            {
                SetFilter(status);
            });
            RuntimeUi.SetRect(button.GetComponent<RectTransform>(), new Vector2(x, 0.86f), new Vector2(160, 54), Vector2.zero);
        }

        private void SetFilter(QuestStatus? status)
        {
            filter = status;
            _ = RefreshQuestsAsync();
        }

        private static string StatusLabel(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Assigned => "Активный",
                QuestStatus.Submitted => "На проверке",
                QuestStatus.Approved => "Одобрен",
                QuestStatus.Rejected => "Отклонён",
                QuestStatus.Claimed => "Получен",
                QuestStatus.Cancelled => "Отменён",
                _ => status.ToString()
            };
        }

        private static Color StatusColor(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Assigned => new Color(0.24f, 0.46f, 0.68f),
                QuestStatus.Submitted => new Color(0.64f, 0.48f, 0.20f),
                QuestStatus.Approved => new Color(0.25f, 0.55f, 0.32f),
                QuestStatus.Rejected => new Color(0.62f, 0.22f, 0.22f),
                QuestStatus.Claimed => new Color(0.38f, 0.33f, 0.58f),
                _ => RuntimeUi.Muted
            };
        }

        private async System.Threading.Tasks.Task OnQuestActionAsync(RealLifeQuest quest)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;
            var childId = ServiceRegistry.Auth.CurrentUser.ChildId;
            try
            {
                if (quest.Status is QuestStatus.Assigned or QuestStatus.Rejected)
                {
                    await ServiceRegistry.Quests.SubmitQuestAsync(childId, quest.Id, "Готово!");
                    statusText.text = "Квест отправлен родителю на проверку.";
                }
                else if (quest.Status == QuestStatus.Approved)
                {
                    var reward = await ServiceRegistry.Quests.ClaimQuestRewardAsync(childId, quest.Id);
                    statusText.text = $"Награда получена: +{reward.Xp} XP, +{reward.Gold} золота.";
                }

                await RefreshQuestsAsync();
            }
            catch (System.Exception ex)
            {
                statusText.text = ex.Message;
            }
            finally
            {
                isBusy = false;
            }
        }
    }
}
