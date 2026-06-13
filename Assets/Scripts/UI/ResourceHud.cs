using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public sealed class ResourceHud : MonoBehaviour
    {
        private Text levelText;
        private Text xpText;
        private Text goldText;
        private RectTransform xpTarget;
        private RectTransform goldTarget;
        private RectTransform animationLayer;
        private GameState pendingState;
        private bool isAnimating;

        public static ResourceHud Create(Transform parent)
        {
            var root = RuntimeUi.SpriteCard(
                parent,
                "ResourceHud",
                ArtLibrary.Panel("ui_panel_dark_wood"),
                new Color(0.14f, 0.11f, 0.09f, 0.96f));
            RuntimeUi.SetRect(root, new Vector2(0.5f, 0.965f), new Vector2(960, 86), Vector2.zero);
            root.SetAsLastSibling();

            var hud = root.gameObject.AddComponent<ResourceHud>();
            hud.Build(root);
            return hud;
        }

        private void Build(RectTransform root)
        {
            var levelIcon = RuntimeUi.SpriteImage(root, "LevelIcon", ArtLibrary.Reward("reward_xp"));
            RuntimeUi.SetRect(levelIcon.rectTransform, new Vector2(0.08f, 0.5f), new Vector2(48, 48), Vector2.zero);
            levelText = RuntimeUi.Text(root, "Level", "", 24, Color.white, TextAnchor.MiddleLeft);
            RuntimeUi.SetRect(levelText.rectTransform, new Vector2(0.19f, 0.5f), new Vector2(190, 62), Vector2.zero);

            xpTarget = CreateResource(root, "Xp", ArtLibrary.Reward("reward_xp"), new Vector2(0.52f, 0.5f), out xpText);
            goldTarget = CreateResource(root, "Gold", ArtLibrary.Reward("reward_gold"), new Vector2(0.82f, 0.5f), out goldText);

            animationLayer = new GameObject("RewardAnimationLayer", typeof(RectTransform)).GetComponent<RectTransform>();
            animationLayer.SetParent(root.parent, false);
            RuntimeUi.Stretch(animationLayer);
            animationLayer.SetAsLastSibling();

            if (ServiceRegistry.GameState != null)
            {
                ServiceRegistry.GameState.Changed += OnStateChanged;
            }
            Refresh(ServiceRegistry.GameState?.Current);
        }

        private static RectTransform CreateResource(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchor,
            out Text valueText)
        {
            var container = RuntimeUi.Card(parent, name, new Color(0.05f, 0.07f, 0.08f, 0.72f));
            RuntimeUi.SetRect(container, anchor, new Vector2(250, 58), Vector2.zero);
            var icon = RuntimeUi.SpriteImage(container, "Icon", sprite);
            RuntimeUi.SetRect(icon.rectTransform, new Vector2(0.18f, 0.5f), new Vector2(44, 44), Vector2.zero);
            valueText = RuntimeUi.Text(container, "Value", "0", 27, Color.white, TextAnchor.MiddleLeft);
            RuntimeUi.SetRect(valueText.rectTransform, new Vector2(0.64f, 0.5f), new Vector2(140, 48), Vector2.zero);
            return container;
        }

        private void OnDestroy()
        {
            if (ServiceRegistry.GameState != null)
            {
                ServiceRegistry.GameState.Changed -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameState state)
        {
            pendingState = state;
            if (!isAnimating)
            {
                Refresh(state);
            }
        }

        public void AnimateReward(RectTransform source, BattleReward reward, GameState beforeState)
        {
            if (reward == null)
            {
                return;
            }

            StopAllCoroutines();
            isAnimating = true;
            pendingState = ServiceRegistry.GameState.Current;
            Refresh(beforeState);
            StartCoroutine(RewardSequence(source, reward));
        }

        private IEnumerator RewardSequence(RectTransform source, BattleReward reward)
        {
            var start = source != null
                ? WorldToLayer(source.TransformPoint(source.rect.center))
                : Vector2.zero;

            const int particleCount = 7;
            for (var i = 0; i < particleCount; i++)
            {
                StartCoroutine(FlyParticle(
                    "XpParticle",
                    ArtLibrary.Reward("reward_xp"),
                    start + Random.insideUnitCircle * 42f,
                    WorldToLayer(xpTarget.position),
                    i * 0.055f));
                StartCoroutine(FlyParticle(
                    "GoldParticle",
                    ArtLibrary.Reward("reward_gold"),
                    start + Random.insideUnitCircle * 42f,
                    WorldToLayer(goldTarget.position),
                    i * 0.065f));
            }

            yield return new WaitForSecondsRealtime(1.05f);
            Refresh(pendingState ?? ServiceRegistry.GameState.Current);
            StartCoroutine(Pulse(xpTarget));
            StartCoroutine(Pulse(goldTarget));
            isAnimating = false;
        }

        private IEnumerator FlyParticle(
            string name,
            Sprite sprite,
            Vector2 start,
            Vector2 target,
            float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            var image = RuntimeUi.SpriteImage(animationLayer, name, sprite);
            var rect = image.rectTransform;
            RuntimeUi.SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(52, 52), start);
            image.raycastTarget = false;

            const float duration = 0.55f;
            var elapsed = 0f;
            var control = (start + target) * 0.5f + new Vector2(Random.Range(-100f, 100f), 150f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = Bezier(start, control, target, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.55f, eased);
                image.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.25f, eased));
                yield return null;
            }

            Destroy(image.gameObject);
        }

        private IEnumerator Pulse(RectTransform target)
        {
            var elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.16f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private Vector2 WorldToLayer(Vector3 worldPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                animationLayer,
                RectTransformUtility.WorldToScreenPoint(null, worldPosition),
                null,
                out var local);
            return local;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * a + 2f * oneMinusT * t * b + t * t * c;
        }

        private void Refresh(GameState state)
        {
            if (state == null)
            {
                return;
            }

            levelText.text = $"Ур. {state.Level}";
            xpText.text = $"{state.Xp} XP";
            goldText.text = state.Gold.ToString();
        }
    }
}
