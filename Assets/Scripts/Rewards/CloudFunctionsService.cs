using System;
using System.Threading.Tasks;

namespace ClickerAdventure
{
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
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (quest.Status != QuestStatus.Approved)
                throw new InvalidOperationException("Награда доступна только после одобрения родителем.");

            var reward = new BattleReward { Xp = quest.RewardXp, Gold = quest.RewardGold };
            if (firebaseRest is { IsSignedIn: true })
            {
                try
                {
                    // Only the server transaction can claim an online quest and credit its reward.
                    await firebaseRest.ClaimQuestRewardAsync(childId, quest.Id);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        "Не удалось подтвердить получение награды. Обновите список заданий и проверьте баланс перед повторной попыткой.",
                        exception);
                }

                try
                {
                    await gameStateService.LoadAsync(childId);
                }
                catch (Exception exception)
                {
                    // The claim succeeded. A failed refresh must never credit the reward again.
                    throw new InvalidOperationException(
                        "Награда получена на сервере, но баланс не обновился. Обновите данные при восстановлении связи.",
                        exception);
                }
                return reward;
            }

            // Local demo mode has no server transaction or remote quest state.
            await Task.Yield();
            await gameStateService.AddBattleRewardAsync(childId, reward);
            return reward;
        }
    }
}
