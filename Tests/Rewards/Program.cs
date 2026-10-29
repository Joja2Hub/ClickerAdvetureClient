using System;
using System.Threading.Tasks;
using ClickerAdventure;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition)
    {
        checks++;
        if (!condition) throw new Exception($"Reward check {checks} failed.");
    }

    private static async Task Main()
    {
        var quest = new RealLifeQuest { Id = "quest-1", Status = QuestStatus.Approved, RewardXp = 35, RewardGold = 20 };
        var remote = new FirebaseRestClient { IsSignedIn = true };
        var state = new GameStateService();
        var service = new CloudFunctionsService(state, remote);
        var reward = await service.ClaimQuestRewardAsync("child-1", quest);
        Check(remote.Claims == 1 && state.Loads == 1 && state.LocalCredits == 0);
        Check(remote.LastChild == "child-1" && remote.LastQuest == "quest-1");
        Check(reward.Xp == 35 && reward.Gold == 20);

        // A request may have committed before its network response was lost.
        var lostResponse = new Exception("Response lost after commit");
        remote.Failure = lostResponse;
        try { await service.ClaimQuestRewardAsync("child-1", quest); Check(false); }
        catch (InvalidOperationException exception)
        {
            Check(exception.InnerException == lostResponse);
            Check(exception.Message.Contains("проверьте баланс"));
        }
        Check(remote.Claims == 2 && state.Loads == 1 && state.LocalCredits == 0);
        Check(quest.Status == QuestStatus.Approved);

        remote.Failure = null;
        state.Failure = new Exception("Balance refresh failed");
        try { await service.ClaimQuestRewardAsync("child-1", quest); Check(false); }
        catch (InvalidOperationException exception)
        {
            Check(exception.InnerException == state.Failure);
            Check(exception.Message.Contains("получена на сервере"));
        }
        Check(remote.Claims == 3 && state.Loads == 2 && state.LocalCredits == 0);

        foreach (QuestStatus status in Enum.GetValues(typeof(QuestStatus)))
        {
            if (status == QuestStatus.Approved) continue;
            quest.Status = status;
            try { await service.ClaimQuestRewardAsync("child-1", quest); Check(false); }
            catch (InvalidOperationException) { Check(true); }
        }
        Check(remote.Claims == 3 && state.LocalCredits == 0);
        try { await service.ClaimQuestRewardAsync("child-1", null); Check(false); }
        catch (ArgumentNullException) { Check(true); }

        // The explicitly local demo flow still awards once through the local game state.
        quest.Status = QuestStatus.Approved;
        var localState = new GameStateService();
        var local = new CloudFunctionsService(localState, new FirebaseRestClient());
        reward = await local.ClaimQuestRewardAsync("demo-child", quest);
        Check(localState.LocalCredits == 1 && localState.Loads == 0);
        Check(ReferenceEquals(reward, localState.LastReward));
        Check(reward.Xp == 35 && reward.Gold == 20);
        Console.WriteLine($"Quest rewards: {checks} checks passed.");
    }
}
