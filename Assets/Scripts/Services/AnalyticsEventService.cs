using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace ClickerAdventure
{
    public sealed class AnalyticsEventService
    {
        private readonly FirebaseRestClient firebaseRest;

        public AnalyticsEventService(FirebaseRestClient firebaseRest)
        {
            this.firebaseRest = firebaseRest;
        }

        public Task TrackQuestViewedAsync(RealLifeQuest quest)
        {
            return TrackSafelyAsync(BuildEvent(quest, "quest_viewed", quest.Status.ToString().ToLowerInvariant(), null));
        }

        public Task TrackQuestSubmittedAsync(RealLifeQuest quest, string previousStatus)
        {
            var analyticsEvent = BuildEvent(quest, "quest_submitted", previousStatus, "submitted");
            analyticsEvent.SubmittedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            analyticsEvent.TimeFromAssignedHours = HoursSince(quest.AssignedAt);
            return TrackSafelyAsync(analyticsEvent);
        }

        public Task TrackQuestClaimedAsync(RealLifeQuest quest)
        {
            var analyticsEvent = BuildEvent(quest, "quest_claimed", "approved", "claimed");
            analyticsEvent.ClaimedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            analyticsEvent.TimeFromAssignedHours = HoursSince(quest.AssignedAt);
            analyticsEvent.TimeFromSubmittedHours = HoursSince(quest.SubmittedAt);
            return TrackSafelyAsync(analyticsEvent);
        }

        public Task TrackLocationCheckInAsync(RealLifeQuest quest, bool success, float distanceMeters)
        {
            var analyticsEvent = BuildEvent(
                quest,
                success ? "location_check_in_success" : "location_check_in_failed",
                null,
                null);
            analyticsEvent.LocationQuest = true;
            analyticsEvent.CheckInDistanceMeters = distanceMeters;
            analyticsEvent.CheckInResult = success ? "success" : "failed";
            return TrackSafelyAsync(analyticsEvent);
        }

        private async Task TrackSafelyAsync(QuestEvent analyticsEvent)
        {
            if (firebaseRest is not { IsSignedIn: true })
            {
                return;
            }

            try
            {
                await firebaseRest.WriteQuestEventAsync(analyticsEvent.ChildId, analyticsEvent);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Quest analytics event '{analyticsEvent.EventType}' was skipped: {ex.Message}");
            }
        }

        private static QuestEvent BuildEvent(
            RealLifeQuest quest,
            string eventType,
            string statusBefore,
            string statusAfter)
        {
            var user = ServiceRegistry.Auth?.CurrentUser;
            var now = DateTime.Now;
            return new QuestEvent
            {
                FamilyId = user?.FamilyId ?? string.Empty,
                ChildId = user?.ChildId ?? string.Empty,
                QuestId = quest?.Id ?? string.Empty,
                EventType = eventType,
                Category = CategoryId(quest?.Category ?? QuestCategory.Custom),
                Difficulty = string.IsNullOrWhiteSpace(quest?.Difficulty) ? "normal" : quest.Difficulty,
                RewardXp = quest?.RewardXp ?? 0,
                RewardGold = quest?.RewardGold ?? 0,
                StatusBefore = statusBefore,
                StatusAfter = statusAfter,
                AssignedAt = quest?.AssignedAt,
                SubmittedAt = quest?.SubmittedAt,
                ApprovedAt = quest?.ApprovedAt,
                ClaimedAt = quest?.ClaimedAt,
                EventAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                LocalDate = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek,
                LocalHour = now.Hour
            };
        }

        private static string CategoryId(QuestCategory category)
        {
            return category == QuestCategory.FamilyHelp
                ? "family_help"
                : category.ToString().ToLowerInvariant();
        }

        private static float HoursSince(string timestamp)
        {
            if (!DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return 0f;
            }

            return Mathf.Max(0f, (float)(DateTime.UtcNow - parsed.ToUniversalTime()).TotalHours);
        }
    }
}
