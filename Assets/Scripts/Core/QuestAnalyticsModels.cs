using System;
using System.Collections.Generic;

namespace ClickerAdventure
{
    public enum QuestEventType
    {
        QuestViewed,
        QuestSubmitted,
        QuestClaimed,
        LocationCheckInSuccess,
        LocationCheckInFailed
    }

    [Serializable]
    public class QuestEvent
    {
        public string FamilyId;
        public string ChildId;
        public string QuestId;
        public string EventType;
        public string ActorRole = "child";
        public string SourceClient = "unity";
        public string Category;
        public string Difficulty = "normal";
        public int RewardXp;
        public int RewardGold;
        public string StatusBefore;
        public string StatusAfter;
        public int AttemptNumber;
        public string AssignedAt;
        public string SubmittedAt;
        public string ApprovedAt;
        public string ClaimedAt;
        public string EventAt;
        public string LocalDate;
        public int DayOfWeek;
        public int LocalHour;
        public float TimeFromAssignedHours;
        public float TimeFromSubmittedHours;
        public bool LocationQuest;
        public float CheckInDistanceMeters;
        public string CheckInResult = "not_required";
        public Dictionary<string, object> Metadata = new();
    }

    [Serializable]
    public class QuestRecommendation
    {
        public string Id;
        public string TargetCategory;
        public string SuggestedDifficulty;
        public int SuggestedRewardXp;
        public int SuggestedRewardGold;
        public int Score;
        public string Label;
        public List<string> Reasons = new();
        public List<string> Risks = new();
        public List<string> SuggestedActions = new();
        public string EngineVersion;
    }

    [Serializable]
    public class QuestAnalyticsSummary
    {
        public int AssignedCount;
        public int ViewedCount;
        public int SubmittedCount;
        public int ApprovedCount;
        public int RejectedCount;
        public int ClaimedCount;
        public float CompletionRate;
        public float ApprovalRate;
        public float AverageSubmitHours;
    }
}
