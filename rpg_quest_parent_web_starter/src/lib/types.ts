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
}

export interface DailyAnalytics {
  id: string;
  questsAssigned: number;
  questsSubmitted: number;
  questsApproved: number;
  questsRejected: number;
  battlesPlayed: number;
  playSessions: number;
  totalPlayTimeSeconds: number;
  xpEarned: number;
  goldEarned: number;
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
