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
            SetSprite(background, backgroundSprite);
            SetSprite(categoryIcon, categorySprite);
            SetSprite(goldIcon, goldSprite);
            SetSprite(xpIcon, xpSprite);
            rewardText.transform.SetAsLastSibling();
            goldIcon.transform.SetAsLastSibling();
            xpIcon.transform.SetAsLastSibling();

            titleText.text = $"{quest.Title}\n{quest.Description}";
            rewardText.text = $"+{quest.RewardGold}\n+{quest.RewardXp}";
            statusText.text = statusLabel;
            statusBackground.color = statusColor;

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
    }
}
