import {
  QuestDifficulty,
  QuestRecommendation,
  RecommendationLabel,
} from "@/lib/types";

export interface RecommendationFeatures {
  category: string;
  assignedTotal: number;
  viewedTotal: number;
  submittedTotal: number;
  approvedTotal: number;
  rejectedTotal: number;
  claimedTotal: number;
  reminderTotal: number;
  completionRate: number;
  approvalRate: number;
  rejectionRate: number;
  viewToSubmitRate: number;
  reminderNeedRate: number;
  avgSubmitHours: number | null;
  avgClaimHours: number | null;
  avgRewardXp: number;
  avgRewardGold: number;
  activeQuestCountToday: number;
  sameCategoryAssignedLast7Days: number;
  preferredLocalHour?: number | null;
}

export interface ExpertRuleResult {
  ruleId: string;
  delta: number;
  reason?: string;
  risk?: string;
  action?: string;
}

export interface ExpertRule {
  id: string;
  title: string;
  apply: (features: RecommendationFeatures) => ExpertRuleResult | null;
}

export interface RecommendationResult
  extends Omit<QuestRecommendation, "id" | "familyId" | "childId" | "targetCategory" | "generatedAt"> {
  label: RecommendationLabel;
  suggestedDifficulty: QuestDifficulty;
}
