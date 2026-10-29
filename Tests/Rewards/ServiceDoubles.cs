using System;
using System.Threading.Tasks;

// Test-only boundaries: the production CloudFunctionsService is compiled unchanged.
namespace ClickerAdventure
{
    public enum QuestStatus { Assigned, Submitted, Approved, Rejected, Claimed, Cancelled }
    public class RealLifeQuest
    {
        public string Id;
        public QuestStatus Status;
        public int RewardXp;
        public int RewardGold;
    }
    public class BattleReward { public int Xp; public int Gold; }
    public class FirebaseRestClient
    {
        public bool IsSignedIn;
        public int Claims;
        public string LastChild;
        public string LastQuest;
        public Exception Failure;
        public Task ClaimQuestRewardAsync(string childId, string questId)
        {
            Claims++;
            LastChild = childId;
            LastQuest = questId;
            return Failure == null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
    public class GameStateService
    {
        public int Loads;
        public int LocalCredits;
        public BattleReward LastReward;
        public Exception Failure;
        public Task LoadAsync(string childId)
        {
            Loads++;
            return Failure == null ? Task.CompletedTask : Task.FromException(Failure);
        }
        public Task AddBattleRewardAsync(string childId, BattleReward reward)
        {
            LocalCredits++;
            LastReward = reward;
            return Task.CompletedTask;
        }
    }
}
