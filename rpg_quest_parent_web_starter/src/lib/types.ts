export type UserRole = "parent" | "child";

export type QuestStatus =
  | "assigned"
  | "submitted"
  | "approved"
  | "rejected"
  | "claimed"
  | "cancelled";

export type QuestCategory =
  | "study"
  | "home"
  | "reading"
  | "health"
  | "sport"
  | "hygiene"
  | "family_help"
  | "custom";

export type QuestDifficulty = "easy" | "normal" | "hard";
export type RecommendationLabel = "recommended" | "neutral" | "not_recommended";
export type QuestEventType =
  | "quest_created"
  | "quest_viewed"
  | "quest_submitted"
  | "quest_approved"
  | "quest_rejected"
  | "quest_claimed"
  | "quest_cancelled"
  | "reminder_sent"
  | "location_check_in_success"
  | "location_check_in_failed";

export interface UserProfile {
  uid: string;
  role: UserRole;
  familyId: string;
  childId?: string;
  displayName: string;
  email?: string;
  createdAt?: unknown;
}

export interface Family {
  id: string;
  name: string;
  ownerParentId: string;
  createdAt?: unknown;
}

export interface ChildProfile {
  id: string;
  familyId: string;
  displayName: string;
  unityLoginEmail?: string;
  unityLoginPasswordForTesting?: string;
  unityAuthUid?: string;
  unityLoginUpdatedAt?: unknown;
  level: number;
  xp: number;
  gold: number;
  createdAt?: unknown;
  updatedAt?: unknown;
}

export interface QuestRewardItem {
  itemId: string;
  quantity: number;
}

export interface Quest {
  id: string;
  title: string;
  description: string;
  category: QuestCategory;
  status: QuestStatus;
  rewardXp: number;
  rewardGold: number;
  rewardItems?: QuestRewardItem[];
  createdByParentId: string;
  createdAt?: unknown;
  dueDate?: unknown;
  submittedAt?: unknown;
  approvedAt?: unknown;
  rejectedAt?: unknown;
  claimedAt?: unknown;
  childComment?: string;
  parentComment?: string;
  proofImageUrl?: string;
  difficulty?: QuestDifficulty;
  recommendationScore?: number;
  recommended?: boolean;
}

export interface QuestEvent {
  id: string;
  familyId: string;
  childId: string;
  questId: string;
  eventType: QuestEventType;
  actorRole: UserRole | "system";
  sourceClient: "web" | "unity" | "cloud_function";
  category: string;
  difficulty?: QuestDifficulty;
  rewardXp: number;
  rewardGold: number;
  statusBefore?: string;
  statusAfter?: string;
  attemptNumber?: number;
  reminderCount?: number;
  assignedAt?: unknown;
  submittedAt?: unknown;
  approvedAt?: unknown;
  claimedAt?: unknown;
  eventAt: unknown;
  localDate: string;
  dayOfWeek: number;
  localHour: number;
  timeFromAssignedHours?: number;
  timeFromSubmittedHours?: number;
  locationQuest?: boolean;
  checkInDistanceMeters?: number;
  checkInResult?: "success" | "failed" | "not_required";
  metadata?: Record<string, unknown>;
}

export interface DailyCategoryStats {
  assigned: number;
  viewed: number;
  submitted: number;
  approved: number;
  rejected: number;
  claimed: number;
  avgSubmitHours: number | null;
  completionRate: number;
  rejectionRate: number;
}

export interface DailyAnalytics {
  id: string;
  date: string;
  familyId: string;
  childId: string;
  assignedCount: number;
  viewedCount: number;
  submittedCount: number;
  approvedCount: number;
  rejectedCount: number;
  claimedCount: number;
  cancelledCount: number;
  reminderCount: number;
  completionRate: number;
  approvalRate: number;
  rejectionRate: number;
  claimRate: number;
  avgSubmitHours: number | null;
  avgClaimHours: number | null;
  activeQuestCount: number;
  categoryStats: Record<string, DailyCategoryStats>;
  updatedAt: unknown;
}

export interface CategoryAnalytics {
  id: string;
  category: string;
  familyId: string;
  childId: string;
  assignedTotal: number;
  viewedTotal: number;
  submittedTotal: number;
  approvedTotal: number;
  rejectedTotal: number;
  claimedTotal: number;
  cancelledTotal: number;
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
  lastAssignedAt?: unknown;
  lastClaimedAt?: unknown;
  recommendationScore: number;
  recommendationLabel: RecommendationLabel;
  recommendationReason: string;
  recommendationActions: string[];
  updatedAt: unknown;
}

export interface QuestRecommendation {
  id: string;
  familyId: string;
  childId: string;
  targetCategory: string;
  suggestedDifficulty: QuestDifficulty;
  suggestedRewardXp: number;
  suggestedRewardGold: number;
  score: number;
  label: RecommendationLabel;
  reasons: string[];
  risks: string[];
  suggestedActions: string[];
  generatedAt: unknown;
  engineVersion: string;
}

export interface GameState {
  level: number;
  xp: number;
  gold: number;
  maxHp: number;
  damage: number;
  critChance: number;
  rageGain: number;
  unlockedLocations: string[];
  completedDungeons: string[];
  currentMapNodeId: string;
  dailyStreak: number;
  lastActiveAt?: unknown;
}
