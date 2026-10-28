using System;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public sealed class QuestCardView : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private Image background;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private Image goldIcon;
        [SerializeField] private Image xpIcon;

        [Header("Text")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text recommendedText;
        [SerializeField] private Text detailsButtonText;
        [SerializeField] private Text actionButtonText;

        [Header("Containers")]
        [SerializeField] private Image statusBackground;
        [SerializeField] private GameObject recommendedBadge;
        [SerializeField] private Button detailsButton;
        [SerializeField] private Button actionButton;

        public RectTransform RectTransform => (RectTransform)transform;
        public RectTransform ActionSource => actionButton != null
            ? actionButton.GetComponent<RectTransform>()
            : RectTransform;

        public void Bind(
            RealLifeQuest quest,
            Sprite backgroundSprite,
            Sprite categorySprite,
            Sprite goldSprite,
            Sprite xpSprite,
            string statusLabel,
            Color statusColor,
            string actionLabel,
            Action onDetails,
            Action<RectTransform> onAction)
        {
            gameObject.name = $"Quest_{quest.Id}";
            UiTheme.ApplySurface(background, UiTheme.Paper);
            SetSprite(categoryIcon, categorySprite);
            SetSprite(goldIcon, goldSprite);
            SetSprite(xpIcon, xpSprite);
            rewardText.transform.SetAsLastSibling();
            goldIcon.transform.SetAsLastSibling();
            xpIcon.transform.SetAsLastSibling();

            titleText.supportRichText = true;
            titleText.text = $"<b>{Escape(quest.Title)}</b>\n<size=22>{Escape(quest.Description)}</size>";
            titleText.color = UiTheme.Ink;
            titleText.fontSize = 28;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            RuntimeUi.SetRect(categoryIcon.rectTransform, new Vector2(0.09f, 0.72f), new Vector2(82, 82), Vector2.zero);
            RuntimeUi.SetRect(titleText.rectTransform, new Vector2(0.55f, 0.70f), new Vector2(590, 110), Vector2.zero);
            RuntimeUi.SetRect(recommendedBadge.GetComponent<RectTransform>(), new Vector2(0.82f, 0.93f), new Vector2(180, 32), Vector2.zero);
            RuntimeUi.SetRect(statusBackground.rectTransform, new Vector2(0.20f, 0.40f), new Vector2(250, 40), Vector2.zero);
            RuntimeUi.SetRect(goldIcon.rectTransform, new Vector2(0.64f, 0.47f), new Vector2(30, 30), Vector2.zero);
            RuntimeUi.SetRect(xpIcon.rectTransform, new Vector2(0.64f, 0.35f), new Vector2(30, 30), Vector2.zero);
            RuntimeUi.SetRect(rewardText.rectTransform, new Vector2(0.82f, 0.41f), new Vector2(240, 76), Vector2.zero);
            rewardText.text = $"+{quest.RewardGold} золота\n+{quest.RewardXp} опыта";
            rewardText.fontSize = 22;
            rewardText.color = UiTheme.SecondaryInk;
            statusText.text = statusLabel;
            UiTheme.ApplySurface(statusBackground, statusColor);
            UiTheme.ApplySurface(recommendedBadge.GetComponent<Image>(), new Color(0.93f, 0.86f, 0.63f));
            recommendedText.color = UiTheme.Ink;
            recommendedText.fontSize = 20;
            foreach (var shadow in GetComponentsInChildren<Shadow>(true)) shadow.enabled = false;
            UiTheme.ApplyButton(detailsButton, UiTheme.Secondary);
            UiTheme.ApplyButton(actionButton, UiTheme.Primary);
            RuntimeUi.SetRect(detailsButton.GetComponent<RectTransform>(), new Vector2(0.27f, 0.15f), new Vector2(350, 62), Vector2.zero);
            RuntimeUi.SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.73f, 0.15f), new Vector2(350, 62), Vector2.zero);
            foreach (var text in new[] { detailsButtonText, actionButtonText, statusText })
            {
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 18;
                text.resizeTextMaxSize = 24;
                RuntimeUi.Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(12, 4);
                text.rectTransform.offsetMax = new Vector2(-12, -4);
            }

            recommendedBadge.SetActive(quest.Recommended || quest.RecommendationScore >= 70);
            recommendedText.text = "Совет дня";

            detailsButtonText.text = "Подробнее";
            detailsButton.onClick.RemoveAllListeners();
            detailsButton.onClick.AddListener(() => onDetails?.Invoke());

            actionButtonText.text = actionLabel;
            actionButton.interactable = quest.Status is QuestStatus.Assigned
                or QuestStatus.Rejected
                or QuestStatus.Approved;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onAction?.Invoke(ActionSource));
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        private static string Escape(string value) => (value ?? string.Empty)
            .Replace("<", "‹").Replace(">", "›");
    }
}
