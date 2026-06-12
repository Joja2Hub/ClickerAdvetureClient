import { expertRules } from "@/lib/recommendations/expertRules";
import {
  RecommendationFeatures,
  RecommendationResult,
} from "@/lib/recommendations/types";
import { QuestDifficulty, RecommendationLabel } from "@/lib/types";

export const RECOMMENDATION_ENGINE_VERSION = "expert-v1";

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function labelForScore(score: number): RecommendationLabel {
  if (score >= 70) return "recommended";
  if (score >= 40) return "neutral";
  return "not_recommended";
}

function difficultyForScore(score: number, assignedTotal: number): QuestDifficulty {
  if (assignedTotal < 3 || score < 40) return "easy";
  if (score >= 85) return "hard";
  return "normal";
}

export function evaluateRecommendation(features: RecommendationFeatures): RecommendationResult {
  const results = expertRules
    .map((rule) => rule.apply(features))
    .filter((result) => result !== null);
  const score = clamp(50 + results.reduce((sum, result) => sum + result.delta, 0), 0, 100);
  const label = labelForScore(score);
  const rewardFactor = label === "not_recommended" ? 0.9 : label === "recommended" ? 1 : 0.95;

  return {
    score,
    label,
    reasons: results.flatMap((result) => result.reason ? [result.reason] : []),
    risks: results.flatMap((result) => result.risk ? [result.risk] : []),
    suggestedActions: results.flatMap((result) => result.action ? [result.action] : []),
    suggestedDifficulty: difficultyForScore(score, features.assignedTotal),
    suggestedRewardXp: Math.max(20, Math.round((features.avgRewardXp || 50) * rewardFactor / 5) * 5),
    suggestedRewardGold: Math.max(10, Math.round((features.avgRewardGold || 25) * rewardFactor / 5) * 5),
    engineVersion: RECOMMENDATION_ENGINE_VERSION,
  };
}
