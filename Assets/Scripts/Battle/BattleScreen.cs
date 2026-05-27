using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class BattleScreen : MonoBehaviour
    {
        private BattleSession session;
        private EnemyConfig currentEnemy;
        private int enemyHp;
        private int playerHp;
        private Text titleText;
        private Text enemyText;
        private Text playerText;
        private Text feedbackText;
        private Button enemyButton;
        private Image enemyHpBar;
        private Image playerHpBar;
        private Image rageBar;
        private RectTransform rewardCard;
        private bool isCompleted;

        private async void Start()
        {
            await ServiceRegistry.InitializeAsync();
            if (!ServiceRegistry.Auth.IsSignedIn)
            {
                await ServiceRegistry.Auth.DevLoginAsync();
                await ServiceRegistry.GameState.LoadAsync(ServiceRegistry.Auth.CurrentUser.ChildId);
            }

            session = ServiceRegistry.Battle.StartSession(SelectionContext.SelectedDungeon);
            playerHp = ServiceRegistry.GameState.Current.MaxHp;
            Build();
            SpawnEnemy();
        }

        private void Update()
        {
            if (!DesktopTestMode.IsEnabled)
            {
                return;
            }

            if (DesktopTestMode.Pressed(KeyCode.Escape))
            {
                SceneLoader.LoadMap();
                return;
            }

            if (isCompleted)
            {
                if (DesktopTestMode.PressedAny(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space))
                {
                    SceneLoader.LoadMap();
                }

                return;
            }

            if (DesktopTestMode.PressedAny(KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter))
            {
                OnEnemyClicked();
            }

            if (DesktopTestMode.Pressed(KeyCode.Q))
            {
                DealDamage(35, true);
            }

            if (DesktopTestMode.Pressed(KeyCode.E))
            {
                HealPlayer();
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.CreateCanvas();
            RuntimeUi.AdventureBackground(canvas.transform, new Color(0.26f, 0.20f, 0.31f), new Color(0.28f, 0.18f, 0.20f), new Color(0.10f, 0.08f, 0.10f));

            var top = RuntimeUi.Card(canvas.transform, "BattleTop", new Color(0.08f, 0.08f, 0.10f, 0.86f));
            RuntimeUi.SetRect(top, new Vector2(0.5f, 0.92f), new Vector2(980, 110), Vector2.zero);
            titleText = RuntimeUi.Text(top, "Title", "", 40, new Color(1f, 0.86f, 0.62f));
            titleText.fontStyle = FontStyle.Bold;
            RuntimeUi.Stretch(titleText.rectTransform);

            var arena = RuntimeUi.Card(canvas.transform, "Arena", new Color(0.16f, 0.11f, 0.13f, 0.82f));
            RuntimeUi.SetRect(arena, new Vector2(0.5f, 0.58f), new Vector2(920, 730), Vector2.zero);

            var enemyOrb = RuntimeUi.Circle(arena, "EnemyOrb", new Color(0.62f, 0.16f, 0.18f));
            RuntimeUi.SetRect(enemyOrb.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(360, 360), Vector2.zero);

            enemyButton = RuntimeUi.Button(arena, "EnemyButton", "Ударить", new Color(0.70f, 0.18f, 0.16f, 0.84f), OnEnemyClicked);
            RuntimeUi.SetRect(enemyButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(390, 150), new Vector2(0, -10));

            enemyText = RuntimeUi.Text(arena, "EnemyText", "", 34, Color.white);
            RuntimeUi.SetRect(enemyText.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(780, 100), Vector2.zero);

            enemyHpBar = RuntimeUi.ProgressBar(arena, "EnemyHp", new Color(0.10f, 0.08f, 0.08f), RuntimeUi.Ruby);
            RuntimeUi.SetRect(enemyHpBar.transform.parent.GetComponent<RectTransform>(), new Vector2(0.5f, 0.20f), new Vector2(680, 42), Vector2.zero);

            var hud = RuntimeUi.Card(canvas.transform, "BattleHud", new Color(0.10f, 0.15f, 0.16f, 0.88f));
            RuntimeUi.SetRect(hud, new Vector2(0.5f, 0.28f), new Vector2(920, 200), Vector2.zero);

            playerText = RuntimeUi.Text(hud, "PlayerText", "", 30, Color.white, TextAnchor.MiddleLeft);
            playerText.rectTransform.offsetMin = new Vector2(32, 98);
            playerText.rectTransform.offsetMax = new Vector2(-32, -14);

            playerHpBar = RuntimeUi.ProgressBar(hud, "PlayerHp", new Color(0.07f, 0.08f, 0.08f), new Color(0.20f, 0.70f, 0.34f));
            RuntimeUi.SetRect(playerHpBar.transform.parent.GetComponent<RectTransform>(), new Vector2(0.35f, 0.31f), new Vector2(470, 38), Vector2.zero);

            rageBar = RuntimeUi.ProgressBar(hud, "Rage", new Color(0.07f, 0.08f, 0.08f), RuntimeUi.Gold);
            RuntimeUi.SetRect(rageBar.transform.parent.GetComponent<RectTransform>(), new Vector2(0.76f, 0.31f), new Vector2(260, 38), Vector2.zero);
            rageBar.fillAmount = 0.35f;

            feedbackText = RuntimeUi.Text(canvas.transform, "Feedback", "", 34, new Color(1f, 0.9f, 0.25f));
            RuntimeUi.SetRect(feedbackText.rectTransform, new Vector2(0.5f, 0.17f), new Vector2(920, 90), Vector2.zero);

            var power = RuntimeUi.Button(canvas.transform, "Power Attack", "Power Attack", new Color(0.45f, 0.25f, 0.65f), () => DealDamage(35, true));
            RuntimeUi.SetRect(power.GetComponent<RectTransform>(), new Vector2(0.25f, 0.08f), new Vector2(300, 76), Vector2.zero);

            var heal = RuntimeUi.Button(canvas.transform, "Heal", "Heal", new Color(0.25f, 0.55f, 0.36f), () =>
            {
                HealPlayer();
            });
            RuntimeUi.SetRect(heal.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), new Vector2(250, 76), Vector2.zero);

            var flee = RuntimeUi.Button(canvas.transform, "Back", "Карта", new Color(0.35f, 0.35f, 0.38f), SceneLoader.LoadMap);
            RuntimeUi.SetRect(flee.GetComponent<RectTransform>(), new Vector2(0.75f, 0.08f), new Vector2(250, 76), Vector2.zero);

            rewardCard = RuntimeUi.Card(canvas.transform, "RewardCard", new Color(0.90f, 0.72f, 0.34f, 0.96f));
            RuntimeUi.SetRect(rewardCard, new Vector2(0.5f, 0.52f), new Vector2(720, 280), Vector2.zero);
            rewardCard.gameObject.SetActive(false);
        }

        private void SpawnEnemy()
        {
            var dungeon = session.Dungeon;
            currentEnemy = dungeon != null && dungeon.EnemyPool.Count > 0
                ? dungeon.EnemyPool[Random.Range(0, dungeon.EnemyPool.Count)]
                : null;

            if (currentEnemy == null)
            {
                feedbackText.text = "Нет врагов в DungeonConfig.";
                enemyButton.interactable = false;
                return;
            }

            enemyHp = currentEnemy.MaxHp;
            StopAllCoroutines();
            StartCoroutine(EnemyAttackLoop());
            feedbackText.text = "Кликайте по врагу и держите комбо.";
            Refresh();
        }

        private void OnEnemyClicked()
        {
            var damage = ServiceRegistry.Battle.RollPlayerDamage(out var critical);
            DealDamage(damage, critical);
        }

        private void DealDamage(int damage, bool strong)
        {
            if (currentEnemy == null || enemyHp <= 0)
            {
                return;
            }

            enemyHp = Mathf.Max(0, enemyHp - damage);
            session.RegisterHit(strong);
            feedbackText.text = strong ? $"Критический удар: -{damage}" : $"-{damage}";

            if (enemyHp <= 0)
            {
                session.RegisterEnemyDefeated(currentEnemy);
                var totalInDungeon = Mathf.Max(1, session.Dungeon.WavesCount * session.Dungeon.EnemiesPerWave);
                if (session.EnemiesDefeated >= totalInDungeon)
                {
                    _ = CompleteVictoryAsync();
                    return;
                }

                var defeatedInWave = session.EnemiesDefeated % Mathf.Max(1, session.Dungeon.EnemiesPerWave);
                if (defeatedInWave == 0)
                {
                    session.AdvanceWave();
                }

                SpawnEnemy();
                return;
            }

            Refresh();
        }

        private IEnumerator EnemyAttackLoop()
        {
            while (currentEnemy != null && enemyHp > 0 && playerHp > 0)
            {
                yield return new WaitForSeconds(currentEnemy.AttackInterval);
                playerHp = Mathf.Max(0, playerHp - currentEnemy.Damage);
                session.ResetCombo();
                feedbackText.text = $"{currentEnemy.DisplayName} атакует: -{currentEnemy.Damage} HP";
                Refresh();

                if (playerHp <= 0)
                {
                    enemyButton.interactable = false;
                    feedbackText.text = "Поражение. Вернитесь на карту и попробуйте снова.";
                }
            }
        }

        private async System.Threading.Tasks.Task CompleteVictoryAsync()
        {
            isCompleted = true;
            enemyButton.interactable = false;
            StopAllCoroutines();
            var childId = ServiceRegistry.Auth.CurrentUser.ChildId;
            await ServiceRegistry.Battle.CompleteVictoryAsync(childId, session);
            await ServiceRegistry.Analytics.TrackBattleCompletedAsync(session.Reward);
            titleText.text = "Победа!";
            enemyText.text = "Данж очищен";
            feedbackText.text = "Награда уже добавлена в gameState.";
            rewardCard.gameObject.SetActive(true);
            var rewardText = RuntimeUi.Text(rewardCard, "RewardText", $"+{session.Reward.Xp} XP\n+{session.Reward.Gold} золота", 42, RuntimeUi.Ink);
            rewardText.fontStyle = FontStyle.Bold;
            RuntimeUi.Stretch(rewardText.rectTransform);
        }

        private void HealPlayer()
        {
            if (isCompleted || playerHp <= 0)
            {
                return;
            }

            playerHp = Mathf.Min(ServiceRegistry.GameState.Current.MaxHp, playerHp + 25);
            feedbackText.text = "+25 HP";
            Refresh();
        }

        private void Refresh()
        {
            titleText.text = $"{session.Dungeon?.DisplayName ?? "Данж"}   Волна {session.CurrentWave}/{session.Dungeon?.WavesCount ?? 1}";
            enemyText.text = $"{currentEnemy.DisplayName}\nHP {enemyHp}/{currentEnemy.MaxHp}";
            playerText.text = $"Герой HP {playerHp}/{ServiceRegistry.GameState.Current.MaxHp}   Combo {session.Combo}   Лучший {session.BestCombo}";
            enemyHpBar.fillAmount = currentEnemy.MaxHp <= 0 ? 0f : enemyHp / (float)currentEnemy.MaxHp;
            playerHpBar.fillAmount = ServiceRegistry.GameState.Current.MaxHp <= 0 ? 0f : playerHp / (float)ServiceRegistry.GameState.Current.MaxHp;
            rageBar.fillAmount = Mathf.Clamp01(session.Combo / 18f);
        }
    }
}
