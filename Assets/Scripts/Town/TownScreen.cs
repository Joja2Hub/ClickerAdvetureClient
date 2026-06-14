using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class TownScreen : MonoBehaviour
    {
        private const float QuestRowHeight = 210f;
        private const float QuestRowGap = 18f;
        private const string QuestCardResourcePath = "Prefabs/UI/QuestCard";

        private RectTransform root;
        private RectTransform questList;
        private RectTransform detailOverlay;
        private Text statusText;
        private ResourceHud resourceHud;
        private QuestCardView questCardPrefab;
        private QuestStatus? filter;
        private List<RealLifeQuest> visibleQuests = new();
        private readonly HashSet<string> viewedQuestIdsThisScreenSession = new();
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
                    _ = OnQuestActionAsync(quest, null);
                }
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.CreateCanvas();
            var guildBackground = ArtLibrary.Background("bg_guild_hall_interior") ?? ArtLibrary.Background("bg_home_village_day");
            if (guildBackground != null)
            {
                RuntimeUi.SpriteBackground(canvas.transform, "TownBackground", guildBackground, new Color(0.12f, 0.13f, 0.12f));
            }
            else
            {
                RuntimeUi.AdventureBackground(canvas.transform, new Color(0.30f, 0.45f, 0.52f), new Color(0.32f, 0.38f, 0.28f), new Color(0.12f, 0.13f, 0.12f));
            }

            root = RuntimeUi.SafeAreaRoot(canvas.transform);
            questCardPrefab = Resources.Load<QuestCardView>(QuestCardResourcePath);
            resourceHud = ResourceHud.Create(root);
            var town = SelectionContext.SelectedTown;
            var title = RuntimeUi.Text(root, "TownTitle", town != null ? town.DisplayName : "Гильдия", 46, new Color(1f, 0.88f, 0.56f));
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.90f), new Vector2(900, 74), Vector2.zero);

            CreateTownActions(root);

            var guildPanel = RuntimeUi.SpriteCard(root, "GuildBoard", ArtLibrary.Panel("ui_panel_dark_wood"), new Color(0.24f, 0.18f, 0.13f, 0.94f));
            RuntimeUi.SetRect(guildPanel, new Vector2(0.5f, 0.50f), new Vector2(960, 1080), Vector2.zero);

            var boardIcon = RuntimeUi.SpriteImage(guildPanel, "NoticeBoardIcon", ArtLibrary.Map("town_notice_board"));
            RuntimeUi.SetRect(boardIcon.rectTransform, new Vector2(0.12f, 0.93f), new Vector2(90, 90), Vector2.zero);

            var subtitle = RuntimeUi.Text(guildPanel, "Subtitle", "Доска приключений", 36, new Color(1f, 0.88f, 0.58f));
            subtitle.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.94f), new Vector2(760, 68), Vector2.zero);

            CreateFilterButton(guildPanel, "Все", null, 0.12f);
            CreateFilterButton(guildPanel, "Активные", QuestStatus.Assigned, 0.30f);
            CreateFilterButton(guildPanel, "Проверка", QuestStatus.Submitted, 0.50f);
            CreateFilterButton(guildPanel, "Одобрено", QuestStatus.Approved, 0.70f);
            CreateFilterButton(guildPanel, "Получено", QuestStatus.Claimed, 0.88f);

            var viewport = RuntimeUi.SpriteCard(guildPanel, "QuestViewport", ArtLibrary.Panel("ui_panel_parchment"), new Color(1f, 0.96f, 0.82f, 0.94f));
            RuntimeUi.SetRect(viewport, new Vector2(0.5f, 0.43f), new Vector2(880, 760), Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45f;

            questList = new GameObject("QuestList", typeof(RectTransform)).GetComponent<RectTransform>();
            questList.SetParent(viewport, false);
            questList.anchorMin = new Vector2(0f, 1f);
            questList.anchorMax = new Vector2(1f, 1f);
            questList.pivot = new Vector2(0.5f, 1f);
            questList.offsetMin = new Vector2(18f, 0f);
            questList.offsetMax = new Vector2(-18f, 0f);
            questList.anchoredPosition = Vector2.zero;
            scroll.content = questList;

            statusText = RuntimeUi.Text(root, "Status", "", 26, new Color(1f, 0.88f, 0.45f));
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.13f), new Vector2(900, 72), Vector2.zero);

            var back = RuntimeUi.Button(root, "BackButton", "Карта", new Color(0.35f, 0.42f, 0.48f), SceneLoader.LoadMap);
            RuntimeUi.SetRect(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(330, 74), Vector2.zero);
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
            var rowHeight = GetQuestRowHeight();
            var contentHeight = Mathf.Max(740f, 36f + visibleQuests.Count * (rowHeight + QuestRowGap));
            questList.sizeDelta = new Vector2(0f, contentHeight);
            questList.anchoredPosition = Vector2.zero;

            for (var i = 0; i < visibleQuests.Count; i++)
            {
                var quest = visibleQuests[i];
                CreateQuestCardFromPrefab(quest, i);
                if (viewedQuestIdsThisScreenSession.Add(quest.Id))
                {
                    _ = ServiceRegistry.AnalyticsEvents.TrackQuestViewedAsync(quest);
                }
            }

            if (visibleQuests.Count == 0)
            {
                var emptyIcon = RuntimeUi.SpriteImage(questList, "EmptyIcon", ArtLibrary.Quest("quest_board_empty"));
                RuntimeUi.SetRect(emptyIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(160, 160), new Vector2(0, -210));
                var empty = RuntimeUi.Text(questList, "Empty", "Здесь пока нет квестов.", 30, RuntimeUi.Ink);
                RuntimeUi.SetRect(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(760, 90), new Vector2(0, -350));
            }
        }

        private void CreateQuestRow(RealLifeQuest quest, int index)
        {
            var row = RuntimeUi.SpriteCard(questList, $"Quest_{quest.Id}", ArtLibrary.Panel("ui_panel_quest_card"), new Color(1f, 0.95f, 0.78f, 0.96f));
            RuntimeUi.SetRect(row, new Vector2(0.5f, 1f), new Vector2(820, QuestRowHeight), new Vector2(0, -120 - index * (QuestRowHeight + QuestRowGap)));

            var icon = RuntimeUi.SpriteImage(row.transform, "QuestIcon", QuestCategoryIcon(quest.Category));
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.12f, 0.60f), new Vector2(106, 106), Vector2.zero);

            var title = RuntimeUi.Text(row.transform, "Title", $"{quest.Title}\n{quest.Description}", 24, RuntimeUi.Ink, TextAnchor.MiddleLeft);
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.42f, 0.62f), new Vector2(470, 118), Vector2.zero);

            if (quest.Recommended || quest.RecommendationScore >= 70)
            {
                var recommended = RuntimeUi.Badge(row, "Recommended", "Совет дня", new Color(0.86f, 0.58f, 0.12f), Color.white);
                RuntimeUi.SetRect(recommended, new Vector2(0.50f, 0.88f), new Vector2(180, 42), Vector2.zero);
            }

            var goldIcon = RuntimeUi.SpriteImage(row.transform, "GoldIcon", ArtLibrary.Reward("reward_gold"));
            RuntimeUi.SetRect(goldIcon.rectTransform, new Vector2(0.71f, 0.75f), new Vector2(44, 44), Vector2.zero);
            var xpIcon = RuntimeUi.SpriteImage(row.transform, "XpIcon", ArtLibrary.Reward("reward_xp"));
            RuntimeUi.SetRect(xpIcon.rectTransform, new Vector2(0.71f, 0.52f), new Vector2(44, 44), Vector2.zero);

            var reward = RuntimeUi.Text(row.transform, "Reward", $"+{quest.RewardGold}\n+{quest.RewardXp}", 22, new Color(0.42f, 0.25f, 0.08f), TextAnchor.MiddleLeft);
            RuntimeUi.SetRect(reward.rectTransform, new Vector2(0.80f, 0.63f), new Vector2(120, 94), Vector2.zero);

            var status = RuntimeUi.Badge(row, "Status", StatusLabel(quest.Status), StatusColor(quest.Status), Color.white);
            RuntimeUi.SetRect(status, new Vector2(0.20f, 0.20f), new Vector2(230, 48), Vector2.zero);

            var details = RuntimeUi.Button(row.transform, "Details", "Подробнее", new Color(0.46f, 0.32f, 0.20f), () => ShowQuestDetails(quest));
            RuntimeUi.SetRect(details.GetComponent<RectTransform>(), new Vector2(0.55f, 0.22f), new Vector2(190, 56), Vector2.zero);

            Button action = null;
            action = RuntimeUi.Button(row.transform, "Action", ActionLabel(quest.Status), new Color(0.36f, 0.45f, 0.70f), () => _ = OnQuestActionAsync(quest, action.GetComponent<RectTransform>()));
            RuntimeUi.SetRect(action.GetComponent<RectTransform>(), new Vector2(0.82f, 0.22f), new Vector2(220, 56), Vector2.zero);
            action.interactable = quest.Status is QuestStatus.Assigned or QuestStatus.Rejected or QuestStatus.Approved;
        }

        private void CreateQuestCardFromPrefab(RealLifeQuest quest, int index)
        {
            if (questCardPrefab == null)
            {
                Debug.LogError($"Quest card prefab is missing at Resources/{QuestCardResourcePath}.");
                return;
            }

            var view = Instantiate(questCardPrefab, questList);
            var rowHeight = GetQuestRowHeight();
            RuntimeUi.SetRect(
                view.RectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(view.RectTransform.sizeDelta.x, rowHeight),
                new Vector2(0, -120 - index * (rowHeight + QuestRowGap)));
            view.Bind(
                quest,
                ArtLibrary.Panel("ui_panel_quest_card"),
                QuestCategoryIcon(quest.Category),
                ArtLibrary.Reward("reward_gold"),
                ArtLibrary.Reward("reward_xp"),
                StatusLabel(quest.Status),
                StatusColor(quest.Status),
                ActionLabel(quest.Status),
                () => ShowQuestDetails(quest),
                source => _ = OnQuestActionAsync(quest, source));
        }

        private float GetQuestRowHeight()
        {
            return questCardPrefab != null
                ? Mathf.Max(1f, questCardPrefab.RectTransform.sizeDelta.y)
                : QuestRowHeight;
        }

        private void ShowQuestDetails(RealLifeQuest quest)
        {
            CloseQuestDetails();

            detailOverlay = RuntimeUi.Panel(root, "QuestDetailsOverlay", new Color(0.02f, 0.025f, 0.03f, 0.90f));
            detailOverlay.SetAsLastSibling();
            detailOverlay.gameObject.AddComponent<Button>().onClick.AddListener(CloseQuestDetails);

            var card = RuntimeUi.SpriteCard(
                detailOverlay,
                "QuestDetailsCard",
                ArtLibrary.Panel("ui_panel_parchment"),
                new Color(1f, 0.97f, 0.86f, 1f));
            RuntimeUi.SetRect(card, new Vector2(0.5f, 0.46f), new Vector2(940, 1420), Vector2.zero);

            var icon = RuntimeUi.SpriteImage(card, "CategoryIcon", QuestCategoryIcon(quest.Category));
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.14f, 0.90f), new Vector2(120, 120), Vector2.zero);

            var title = RuntimeUi.Text(card, "Title", quest.Title, 42, RuntimeUi.Ink, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.58f, 0.91f), new Vector2(690, 120), Vector2.zero);

            var status = RuntimeUi.Badge(card, "Status", StatusLabel(quest.Status), StatusColor(quest.Status), Color.white);
            RuntimeUi.SetRect(status, new Vector2(0.22f, 0.81f), new Vector2(280, 58), Vector2.zero);
            var difficulty = RuntimeUi.Badge(card, "Difficulty", DifficultyLabel(quest.Difficulty), new Color(0.42f, 0.34f, 0.26f), Color.white);
            RuntimeUi.SetRect(difficulty, new Vector2(0.57f, 0.81f), new Vector2(240, 58), Vector2.zero);

            var descriptionPanel = RuntimeUi.Card(card, "DescriptionPanel", new Color(1f, 1f, 1f, 0.35f));
            RuntimeUi.SetRect(descriptionPanel, new Vector2(0.5f, 0.58f), new Vector2(820, 520), Vector2.zero);
            descriptionPanel.gameObject.AddComponent<RectMask2D>();
            var descriptionScroll = descriptionPanel.gameObject.AddComponent<ScrollRect>();
            descriptionScroll.horizontal = false;
            descriptionScroll.movementType = ScrollRect.MovementType.Clamped;
            descriptionScroll.scrollSensitivity = 38f;

            var description = RuntimeUi.Text(descriptionPanel, "Description", quest.Description, 31, RuntimeUi.Ink, TextAnchor.UpperLeft);
            var descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = new Vector2(0f, 1f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.pivot = new Vector2(0.5f, 1f);
            descriptionRect.offsetMin = new Vector2(34, 0);
            descriptionRect.offsetMax = new Vector2(-34, 0);
            descriptionRect.sizeDelta = new Vector2(-68, Mathf.Max(470f, description.preferredHeight + 60f));
            descriptionRect.anchoredPosition = new Vector2(0, -28);
            descriptionScroll.content = descriptionRect;

            var rewardTitle = RuntimeUi.Text(card, "RewardTitle", "Награда", 30, RuntimeUi.Ink);
            rewardTitle.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(rewardTitle.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(500, 60), Vector2.zero);

            var xpIcon = RuntimeUi.SpriteImage(card, "XpIcon", ArtLibrary.Reward("reward_xp"));
            RuntimeUi.SetRect(xpIcon.rectTransform, new Vector2(0.29f, 0.29f), new Vector2(76, 76), Vector2.zero);
            var xp = RuntimeUi.Text(card, "Xp", $"+{quest.RewardXp} XP", 31, RuntimeUi.Ink);
            RuntimeUi.SetRect(xp.rectTransform, new Vector2(0.41f, 0.29f), new Vector2(210, 70), Vector2.zero);

            var goldIcon = RuntimeUi.SpriteImage(card, "GoldIcon", ArtLibrary.Reward("reward_gold"));
            RuntimeUi.SetRect(goldIcon.rectTransform, new Vector2(0.62f, 0.29f), new Vector2(76, 76), Vector2.zero);
            var gold = RuntimeUi.Text(card, "Gold", $"+{quest.RewardGold}", 31, RuntimeUi.Ink);
            RuntimeUi.SetRect(gold.rectTransform, new Vector2(0.73f, 0.29f), new Vector2(160, 70), Vector2.zero);

            var comment = BuildComment(quest);
            if (!string.IsNullOrWhiteSpace(comment))
            {
                var commentText = RuntimeUi.Text(card, "Comment", comment, 25, new Color(0.38f, 0.24f, 0.13f), TextAnchor.UpperLeft);
                RuntimeUi.SetRect(commentText.rectTransform, new Vector2(0.5f, 0.18f), new Vector2(800, 150), Vector2.zero);
            }

            var close = RuntimeUi.Button(card, "Close", "Закрыть", new Color(0.38f, 0.34f, 0.31f), CloseQuestDetails);
            RuntimeUi.SetRect(close.GetComponent<RectTransform>(), new Vector2(0.28f, 0.07f), new Vector2(300, 76), Vector2.zero);

            Button action = null;
            action = RuntimeUi.Button(card, "Action", ActionLabel(quest.Status), new Color(0.31f, 0.48f, 0.71f), () => _ = OnQuestActionAsync(quest, action.GetComponent<RectTransform>()));
            RuntimeUi.SetRect(action.GetComponent<RectTransform>(), new Vector2(0.70f, 0.07f), new Vector2(360, 76), Vector2.zero);
            action.interactable = quest.Status is QuestStatus.Assigned or QuestStatus.Rejected or QuestStatus.Approved;

            resourceHud.transform.SetAsLastSibling();
        }

        private void CloseQuestDetails()
        {
            if (detailOverlay == null)
            {
                return;
            }

            Destroy(detailOverlay.gameObject);
            detailOverlay = null;
        }

        private static string BuildComment(RealLifeQuest quest)
        {
            if (!string.IsNullOrWhiteSpace(quest.ParentComment))
            {
                return $"Комментарий родителя:\n{quest.ParentComment}";
            }
            if (!string.IsNullOrWhiteSpace(quest.ChildComment))
            {
                return $"Комментарий ребенка:\n{quest.ChildComment}";
            }
            return string.Empty;
        }

        private static string DifficultyLabel(string difficulty)
        {
            return difficulty switch
            {
                "easy" => "Легкое",
                "hard" => "Сложное",
                _ => "Обычное"
            };
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
                RuntimeUi.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.18f + i * 0.21f, 0.84f), new Vector2(190, 62), Vector2.zero);
            }
        }

        private void CreateFilterButton(Transform parent, string label, QuestStatus? status, float x)
        {
            var button = RuntimeUi.Button(parent, $"Filter_{label}", label, new Color(0.25f, 0.34f, 0.38f), () =>
            {
                SetFilter(status);
            });
            RuntimeUi.SetRect(button.GetComponent<RectTransform>(), new Vector2(x, 0.86f), new Vector2(160, 52), Vector2.zero);
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
                QuestStatus.Rejected => "Отклонен",
                QuestStatus.Claimed => "Получен",
                QuestStatus.Cancelled => "Отменен",
                _ => status.ToString()
            };
        }

        private static string ActionLabel(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Assigned or QuestStatus.Rejected => "На проверку",
                QuestStatus.Approved => "Забрать",
                QuestStatus.Submitted => "Ждем",
                QuestStatus.Claimed => "Получено",
                _ => "Недоступно"
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

        private static Sprite QuestCategoryIcon(QuestCategory category)
        {
            return ArtLibrary.Quest(category switch
            {
                QuestCategory.Reading => "quest_icon_reading",
                QuestCategory.Study => "quest_icon_study",
                QuestCategory.Home => "quest_icon_home",
                QuestCategory.Health => "quest_icon_health",
                QuestCategory.Sport => "quest_icon_sport",
                QuestCategory.Hygiene => "quest_icon_hygiene",
                QuestCategory.FamilyHelp => "quest_icon_family_help",
                _ => "quest_icon_custom"
            });
        }

        private static Sprite StatusStamp(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Submitted => ArtLibrary.Quest("quest_stamp_submitted"),
                QuestStatus.Approved => ArtLibrary.Quest("quest_stamp_approved"),
                QuestStatus.Rejected => ArtLibrary.Quest("quest_stamp_rejected"),
                QuestStatus.Claimed => ArtLibrary.Panel("ui_badge_claimed"),
                _ => ArtLibrary.Quest("quest_icon_scroll")
            };
        }

        private async System.Threading.Tasks.Task OnQuestActionAsync(RealLifeQuest quest, RectTransform rewardSource)
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
                    var previousStatus = quest.Status.ToString().ToLowerInvariant();
                    await ServiceRegistry.Quests.SubmitQuestAsync(childId, quest.Id, "Готово!");
                    _ = ServiceRegistry.AnalyticsEvents.TrackQuestSubmittedAsync(quest, previousStatus);
                    statusText.text = "Квест отправлен родителю на проверку.";
                }
                else if (quest.Status == QuestStatus.Approved)
                {
                    var beforeState = ServiceRegistry.GameState.Current.Clone();
                    var reward = await ServiceRegistry.Quests.ClaimQuestRewardAsync(childId, quest.Id);
                    _ = ServiceRegistry.AnalyticsEvents.TrackQuestClaimedAsync(quest);
                    resourceHud.AnimateReward(rewardSource, reward, beforeState);
                    statusText.text = $"Награда получена: +{reward.Xp} XP, +{reward.Gold} золота.";
                }

                CloseQuestDetails();
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
