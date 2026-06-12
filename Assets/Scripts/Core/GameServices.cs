using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClickerAdventure
{
    public static class ServiceRegistry
    {
        public static FirebaseInitializer Firebase { get; private set; }
        public static FirebaseRestClient FirebaseRest { get; private set; }
        public static AuthService Auth { get; private set; }
        public static PlayerProfileService PlayerProfiles { get; private set; }
        public static GameStateService GameState { get; private set; }
        public static QuestService Quests { get; private set; }
        public static RewardService Rewards { get; private set; }
        public static InventoryService Inventory { get; private set; }
        public static EquipmentService Equipment { get; private set; }
        public static MapService Map { get; private set; }
        public static BattleService Battle { get; private set; }
        public static AnalyticsService Analytics { get; private set; }
        public static AnalyticsEventService AnalyticsEvents { get; private set; }
        public static CloudFunctionsService CloudFunctions { get; private set; }
        public static LevelService Levels { get; private set; }
        public static NetworkStateService Network { get; private set; }

        public static bool IsReady { get; private set; }

        public static async Task InitializeAsync()
        {
            if (IsReady)
            {
                return;
            }

            Firebase = new FirebaseInitializer();
            await Firebase.InitializeAsync();
            FirebaseRest = new FirebaseRestClient(Firebase.WebConfig);
            Auth = new AuthService(FirebaseRest);
            PlayerProfiles = new PlayerProfileService(FirebaseRest);
            GameState = new GameStateService(FirebaseRest);
            CloudFunctions = new CloudFunctionsService(GameState, FirebaseRest);
            Quests = new QuestService(CloudFunctions, FirebaseRest);
            Rewards = new RewardService(GameState);
            Inventory = new InventoryService();
            Equipment = new EquipmentService(Inventory);
            Map = new MapService();
            Battle = new BattleService(GameState, Rewards);
            Analytics = new AnalyticsService();
            AnalyticsEvents = new AnalyticsEventService(FirebaseRest);
            Levels = new LevelService();
            Network = new NetworkStateService();

            IsReady = true;
        }
    }

    public static class SelectionContext
    {
        public static DungeonConfig SelectedDungeon;
        public static TownConfig SelectedTown;
        public static BattleReward LastReward;
    }

    public sealed class SceneLoader
    {
        public const string Boot = "00_Boot";
        public const string Login = "01_Login";
        public const string Map = "02_Map";
        public const string Battle = "03_Battle";
        public const string Town = "04_Town";
        public const string Loading = "05_Loading";

        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public static void LoadMap() => Load(Map);
        public static void LoadLogin() => Load(Login);
        public static void LoadBattle(DungeonConfig dungeon)
        {
            SelectionContext.SelectedDungeon = dungeon;
            Load(Battle);
        }

        public static void LoadTown(TownConfig town)
        {
            SelectionContext.SelectedTown = town;
            Load(Town);
        }
    }

    public sealed class FirebaseInitializer
    {
        public bool IsAvailable { get; private set; }
        public FirebaseWebConfig WebConfig { get; private set; }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            WebConfig = FirebaseWebConfig.LoadForLocalTesting();
            IsAvailable = WebConfig.IsValid;
            Debug.Log(IsAvailable
                ? "Firebase REST bridge is configured for parent web project."
                : "Firebase config is missing. Running in dev/offline mode.");
        }
    }

    public sealed class AuthService
    {
        private readonly FirebaseRestClient firebaseRest;

        public UserProfile CurrentUser { get; private set; }
        public bool IsSignedIn => CurrentUser != null;

        public AuthService(FirebaseRestClient firebaseRest)
        {
            this.firebaseRest = firebaseRest;
        }

        public async Task<UserProfile> SignInWithEmailAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Введите email ребёнка.");
            }

            if (firebaseRest is { IsConfigured: true })
            {
                CurrentUser = await firebaseRest.SignInChildAsync(email, password);
                return CurrentUser;
            }

            await Task.Yield();
            CurrentUser = CreateDevUser(email);
            return CurrentUser;
        }

        public async Task<UserProfile> DevLoginAsync()
        {
            await Task.Yield();
            CurrentUser = CreateDevUser("child.demo@example.com");
            return CurrentUser;
        }

        public void SignOut()
        {
            firebaseRest?.SignOut();
            CurrentUser = null;
        }

        private static UserProfile CreateDevUser(string email)
        {
            return new UserProfile
            {
                Uid = "dev-child-uid",
                Role = UserRole.Child,
                FamilyId = "demo-family",
                ChildId = "demo-child",
                DisplayName = "Юный герой",
                Email = email
            };
        }
    }

    public sealed class PlayerProfileService
    {
        private readonly FirebaseRestClient firebaseRest;

        public ChildProfile CurrentChild { get; private set; }

        public PlayerProfileService(FirebaseRestClient firebaseRest)
        {
            this.firebaseRest = firebaseRest;
        }

        public async Task<ChildProfile> LoadChildProfileAsync(string childId)
        {
            if (firebaseRest is { IsSignedIn: true })
            {
                CurrentChild = await firebaseRest.LoadChildProfileAsync(childId);
                return CurrentChild;
            }

            await Task.Yield();
            CurrentChild = new ChildProfile
            {
                Id = childId,
                FamilyId = "demo-family",
                DisplayName = "Юный герой",
                Level = 1,
                Xp = 0,
                Gold = 25
            };
            return CurrentChild;
        }
    }

    public sealed class GameStateService
    {
        private readonly FirebaseRestClient firebaseRest;

        public GameState Current { get; private set; }
        public event Action<GameState> Changed;

        public GameStateService(FirebaseRestClient firebaseRest)
        {
            this.firebaseRest = firebaseRest;
        }

        public async Task<GameState> LoadAsync(string childId)
        {
            if (firebaseRest is { IsSignedIn: true })
            {
                Current = await firebaseRest.LoadGameStateAsync(childId);
                EnsureDefaultUnlocks(Current);
                Changed?.Invoke(Current);
                return Current;
            }

            await Task.Yield();
            Current ??= new GameState
            {
                Level = 1,
                Xp = 0,
                Gold = 25,
                MaxHp = 100,
                Damage = 12,
                CritChance = 0.12f,
                RageGain = 16f,
                UnlockedLocations = new List<string> { "sunny_forest", "home_village" },
                CurrentMapNodeId = "sunny_forest"
            };
            Changed?.Invoke(Current);
            return Current;
        }

        public async Task SaveAsync(string childId, GameState state)
        {
            if (firebaseRest is { IsSignedIn: true })
            {
                await firebaseRest.TrySaveGameStateAsync(childId, state);
            }

            await Task.Yield();
            Current = state.Clone();
            Changed?.Invoke(Current);
        }

        public async Task AddBattleRewardAsync(string childId, BattleReward reward)
        {
            var state = Current ?? await LoadAsync(childId);
            state.Xp += reward.Xp;
            state.Gold += reward.Gold;
            ServiceRegistry.Levels.TryLevelUp(state, out _);
            await SaveAsync(childId, state);
        }

        private static void EnsureDefaultUnlocks(GameState state)
        {
            state.UnlockedLocations ??= new List<string>();
            state.CompletedDungeons ??= new List<string>();
            if (state.UnlockedLocations.Count == 0)
            {
                state.UnlockedLocations.Add("sunny_forest");
                state.UnlockedLocations.Add("home_village");
            }
        }
    }

    public sealed class QuestService
    {
        private readonly CloudFunctionsService cloudFunctions;
        private readonly FirebaseRestClient firebaseRest;
        private readonly List<RealLifeQuest> quests = new()
        {
            new RealLifeQuest
            {
                Id = "quest-reading-01",
                Title = "Прочитать 15 минут",
                Description = "Почитай книгу и напиши, что было самым интересным.",
                Category = QuestCategory.Reading,
                Status = QuestStatus.Assigned,
                RewardXp = 40,
                RewardGold = 12
            },
            new RealLifeQuest
            {
                Id = "quest-home-01",
                Title = "Убрать рабочий стол",
                Description = "Наведи порядок на столе и отправь комментарий.",
                Category = QuestCategory.Home,
                Status = QuestStatus.Approved,
                RewardXp = 30,
                RewardGold = 10
            }
        };

        public QuestService(CloudFunctionsService cloudFunctions, FirebaseRestClient firebaseRest)
        {
            this.cloudFunctions = cloudFunctions;
            this.firebaseRest = firebaseRest;
        }

        public event Action<List<RealLifeQuest>> Changed;

        public async Task<List<RealLifeQuest>> LoadQuestsAsync(string childId)
        {
            if (firebaseRest is { IsSignedIn: true })
            {
                return await firebaseRest.LoadQuestsAsync(childId);
            }

            await Task.Yield();
            return quests.Select(CloneQuest).ToList();
        }

        public void ListenQuests(string childId, Action<List<RealLifeQuest>> onChanged)
        {
            Changed += onChanged;
            onChanged?.Invoke(quests.Select(CloneQuest).ToList());
        }

        public async Task SubmitQuestAsync(string childId, string questId, string childComment)
        {
            if (firebaseRest is { IsSignedIn: true })
            {
                await firebaseRest.SubmitQuestAsync(childId, questId, childComment);
                Changed?.Invoke(await firebaseRest.LoadQuestsAsync(childId));
                return;
            }

            await Task.Yield();
            var quest = quests.FirstOrDefault(q => q.Id == questId);
            if (quest == null)
            {
                throw new InvalidOperationException("Квест не найден.");
            }

            if (quest.Status is not (QuestStatus.Assigned or QuestStatus.Rejected))
            {
                throw new InvalidOperationException("Этот квест нельзя отправить на проверку.");
            }

            quest.Status = QuestStatus.Submitted;
            quest.ChildComment = childComment;
            Changed?.Invoke(quests.Select(CloneQuest).ToList());
        }

        public async Task<BattleReward> ClaimQuestRewardAsync(string childId, string questId)
        {
            if (firebaseRest is { IsSignedIn: true })
            {
                var liveQuests = await firebaseRest.LoadQuestsAsync(childId);
                var liveQuest = liveQuests.FirstOrDefault(q => q.Id == questId);
                if (liveQuest == null)
                {
                    throw new InvalidOperationException("Квест не найден.");
                }

                var liveReward = await cloudFunctions.ClaimQuestRewardAsync(childId, liveQuest);
                Changed?.Invoke(await firebaseRest.LoadQuestsAsync(childId));
                return liveReward;
            }

            var quest = quests.FirstOrDefault(q => q.Id == questId);
            if (quest == null)
            {
                throw new InvalidOperationException("Квест не найден.");
            }

            if (quest.Status != QuestStatus.Approved)
            {
                throw new InvalidOperationException("Награда доступна только после одобрения родителем.");
            }

            var reward = await cloudFunctions.ClaimQuestRewardAsync(childId, quest);
            quest.Status = QuestStatus.Claimed;
            Changed?.Invoke(quests.Select(CloneQuest).ToList());
            return reward;
        }

        private static RealLifeQuest CloneQuest(RealLifeQuest quest)
        {
            return new RealLifeQuest
            {
                Id = quest.Id,
                Title = quest.Title,
                Description = quest.Description,
                Category = quest.Category,
                Status = quest.Status,
                RewardXp = quest.RewardXp,
                RewardGold = quest.RewardGold,
                ChildComment = quest.ChildComment,
                ParentComment = quest.ParentComment,
                ProofImageUrl = quest.ProofImageUrl,
                Difficulty = quest.Difficulty,
                RecommendationScore = quest.RecommendationScore,
                Recommended = quest.Recommended,
                AssignedAt = quest.AssignedAt,
                SubmittedAt = quest.SubmittedAt,
                ApprovedAt = quest.ApprovedAt,
                ClaimedAt = quest.ClaimedAt
            };
        }
    }

    public sealed class CloudFunctionsService
    {
        private readonly GameStateService gameStateService;
        private readonly FirebaseRestClient firebaseRest;

        public CloudFunctionsService(GameStateService gameStateService, FirebaseRestClient firebaseRest)
        {
            this.gameStateService = gameStateService;
            this.firebaseRest = firebaseRest;
        }

        public async Task<BattleReward> ClaimQuestRewardAsync(string childId, RealLifeQuest quest)
        {
            var reward = new BattleReward { Xp = quest.RewardXp, Gold = quest.RewardGold };
            if (firebaseRest is { IsSignedIn: true })
            {
                try
                {
                    await firebaseRest.ClaimQuestRewardAsync(childId, quest.Id);
                    await gameStateService.LoadAsync(childId);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Cloud reward claim failed, using direct Firestore MVP flow: {ex.Message}");
                    await firebaseRest.ClaimQuestRewardDirectAsync(childId, quest.Id);
                    await gameStateService.AddBattleRewardAsync(childId, reward);
                }

                return reward;
            }

            await Task.Yield();
            await gameStateService.AddBattleRewardAsync(childId, reward);
            return reward;
        }
    }

    public sealed class RewardService
    {
        private readonly GameStateService gameStateService;

        public RewardService(GameStateService gameStateService)
        {
            this.gameStateService = gameStateService;
        }

        public async Task ApplyBattleRewardAsync(string childId, BattleReward reward)
        {
            await gameStateService.AddBattleRewardAsync(childId, reward);
            SelectionContext.LastReward = reward;
        }
    }

    public sealed class InventoryService
    {
        private readonly List<InventoryItemInstance> items = new();

        public async Task<List<InventoryItemInstance>> LoadAsync(string childId)
        {
            await Task.Yield();
            if (items.Count == 0)
            {
                items.Add(new InventoryItemInstance
                {
                    InstanceId = "starter-sword",
                    ItemId = "wooden_sword",
                    ItemType = ItemType.Weapon,
                    Rarity = Rarity.Common,
                    Quantity = 1,
                    Slot = EquipmentSlot.Weapon
                });
            }

            return items.ToList();
        }

        public async Task SetEquippedAsync(string itemInstanceId, bool equipped)
        {
            await Task.Yield();
            var item = items.FirstOrDefault(x => x.InstanceId == itemInstanceId);
            if (item != null)
            {
                item.Equipped = equipped;
            }
        }
    }

    public sealed class EquipmentService
    {
        private readonly InventoryService inventoryService;

        public EquipmentService(InventoryService inventoryService)
        {
            this.inventoryService = inventoryService;
        }

        public async Task EquipAsync(string itemInstanceId)
        {
            await inventoryService.SetEquippedAsync(itemInstanceId, true);
        }
    }

    public sealed class MapService
    {
        public IReadOnlyList<MapNodeState> BuildNodeStates(IEnumerable<MapNodeConfig> nodes, GameState state)
        {
            if (nodes == null)
            {
                return Array.Empty<MapNodeState>();
            }

            return nodes
                .Where(node => node != null)
                .Select(node => new MapNodeState(node, IsUnlocked(node, state)))
                .ToList();
        }

        public bool IsUnlocked(MapNodeConfig node, GameState state)
        {
            if (node == null || state == null)
            {
                return false;
            }

            return node.RequiredLevel <= state.Level || state.UnlockedLocations.Contains(node.NodeId);
        }
    }

    public sealed class BattleService
    {
        private readonly GameStateService gameStateService;
        private readonly RewardService rewardService;

        public BattleService(GameStateService gameStateService, RewardService rewardService)
        {
            this.gameStateService = gameStateService;
            this.rewardService = rewardService;
        }

        public BattleSession StartSession(DungeonConfig dungeon)
        {
            return new BattleSession(dungeon);
        }

        public int RollPlayerDamage(out bool critical)
        {
            var state = gameStateService.Current ?? new GameState();
            critical = UnityEngine.Random.value < state.CritChance;
            var damage = state.Damage;
            return critical ? Mathf.RoundToInt(damage * 1.8f) : damage;
        }

        public async Task CompleteVictoryAsync(string childId, BattleSession session)
        {
            session.AddPerformanceBonus();
            if (session.Dungeon != null && session.Dungeon.LootTable != null)
            {
                var itemId = session.Dungeon.LootTable.Roll();
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    session.Reward.ItemIds.Add(itemId);
                }
            }

            await rewardService.ApplyBattleRewardAsync(childId, session.Reward);
        }
    }

    public sealed class AnalyticsService
    {
        public int BattlesPlayed { get; private set; }
        public int XpEarned { get; private set; }
        public int GoldEarned { get; private set; }

        public async Task TrackBattleCompletedAsync(BattleReward reward)
        {
            await Task.Yield();
            BattlesPlayed++;
            XpEarned += reward.Xp;
            GoldEarned += reward.Gold;
        }
    }

    public sealed class LevelService
    {
        private readonly int[] xpTable = { 0, 0, 100, 250, 450, 700 };

        public int GetLevelForXp(int xp)
        {
            var level = 1;
            for (var i = 1; i < xpTable.Length; i++)
            {
                if (xp >= xpTable[i])
                {
                    level = i;
                }
            }

            return level;
        }

        public int GetXpRequiredForLevel(int level)
        {
            if (level < 1)
            {
                return 0;
            }

            if (level >= xpTable.Length)
            {
                return xpTable[^1] + (level - xpTable.Length + 1) * 350;
            }

            return xpTable[level];
        }

        public bool TryLevelUp(GameState state, out int newLevel)
        {
            newLevel = GetLevelForXp(state.Xp);
            if (newLevel <= state.Level)
            {
                return false;
            }

            state.Level = newLevel;
            return true;
        }
    }

    public sealed class NetworkStateService
    {
        public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;
    }
}
