import {
  collection,
  doc,
  getDoc,
  getDocs,
  limit,
  orderBy,
  query,
  serverTimestamp,
  setDoc,
  Timestamp,
  writeBatch,
} from "firebase/firestore";
import { httpsCallable } from "firebase/functions";
import { db, functions } from "@/lib/firebase/client";
import { evaluateRecommendation } from "@/lib/recommendations/recommendationEngine";
import { RecommendationFeatures } from "@/lib/recommendations/types";
import {
  CategoryAnalytics,
  DailyAnalytics,
  DailyCategoryStats,
  QuestEvent,
  QuestRecommendation,
} from "@/lib/types";

type TrackQuestEventInput = Omit<
  QuestEvent,
  "id" | "eventAt" | "localDate" | "dayOfWeek" | "localHour"
>;

type MutableStats = {
  assigned: number;
  viewed: number;
  submitted: number;
  approved: number;
  rejected: number;
  claimed: number;
  cancelled: number;
  reminders: number;
  submitHours: number[];
  claimHours: number[];
  rewardXp: number[];
  rewardGold: number[];
  lastAssignedAt?: Timestamp;
  lastClaimedAt?: Timestamp;
};

const emptyStats = (): MutableStats => ({
  assigned: 0,
  viewed: 0,
  submitted: 0,
  approved: 0,
  rejected: 0,
  claimed: 0,
  cancelled: 0,
  reminders: 0,
  submitHours: [],
  claimHours: [],
  rewardXp: [],
  rewardGold: [],
});

function localDateParts(now = new Date()) {
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return {
    localDate: `${year}-${month}-${day}`,
    dayOfWeek: now.getDay() === 0 ? 7 : now.getDay(),
    localHour: now.getHours(),
  };
}

function numberOrZero(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function timestampOrUndefined(value: unknown): Timestamp | undefined {
  if (value instanceof Timestamp) return value;
  if (value && typeof value === "object" && "toDate" in value) return value as Timestamp;
  return undefined;
}

function average(values: number[]): number | null {
  if (values.length === 0) return null;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function rate(numerator: number, denominator: number): number {
  return denominator > 0 ? numerator / denominator : 0;
}

function compact<T extends Record<string, unknown>>(value: T): T {
  return Object.fromEntries(Object.entries(value).filter(([, item]) => item !== undefined)) as T;
}

function applyEvent(stats: MutableStats, event: QuestEvent) {
  switch (event.eventType) {
    case "quest_created":
      stats.assigned++;
      stats.rewardXp.push(numberOrZero(event.rewardXp));
      stats.rewardGold.push(numberOrZero(event.rewardGold));
      stats.lastAssignedAt = timestampOrUndefined(event.eventAt) ?? stats.lastAssignedAt;
      break;
    case "quest_viewed":
      stats.viewed++;
      break;
    case "quest_submitted":
      stats.submitted++;
      if (typeof event.timeFromAssignedHours === "number") stats.submitHours.push(event.timeFromAssignedHours);
      break;
    case "quest_approved":
      stats.approved++;
      break;
    case "quest_rejected":
      stats.rejected++;
      break;
    case "quest_claimed":
      stats.claimed++;
      if (typeof event.timeFromAssignedHours === "number") stats.claimHours.push(event.timeFromAssignedHours);
      stats.lastClaimedAt = timestampOrUndefined(event.eventAt) ?? stats.lastClaimedAt;
      break;
    case "quest_cancelled":
      stats.cancelled++;
      break;
    case "reminder_sent":
      stats.reminders++;
      break;
  }
}

function dailyCategoryStats(stats: MutableStats): DailyCategoryStats {
  return {
    assigned: stats.assigned,
    viewed: stats.viewed,
    submitted: stats.submitted,
    approved: stats.approved,
    rejected: stats.rejected,
    claimed: stats.claimed,
    avgSubmitHours: average(stats.submitHours),
    completionRate: rate(stats.claimed, stats.assigned),
    rejectionRate: rate(stats.rejected, stats.submitted),
  };
}

export async function trackQuestEvent(event: TrackQuestEventInput): Promise<void> {
  const now = new Date();
  const parts = localDateParts(now);
  await setDoc(doc(collection(db, "children", event.childId, "questEvents")), compact({
    ...event,
    eventAt: serverTimestamp(),
    ...parts,
  }));
}

export async function getDailyAnalytics(childId: string, date: string): Promise<DailyAnalytics | null> {
  const snap = await getDoc(doc(db, "children", childId, "analyticsDaily", date));
  return snap.exists() ? ({ id: snap.id, ...snap.data() } as DailyAnalytics) : null;
}

export async function listRecentDailyAnalytics(childId: string, days = 14): Promise<DailyAnalytics[]> {
  const snap = await getDocs(
    query(collection(db, "children", childId, "analyticsDaily"), orderBy("__name__", "desc"), limit(days))
  );
  return snap.docs.map((item) => ({ id: item.id, ...item.data() } as DailyAnalytics)).reverse();
}

export async function getCategoryAnalytics(childId: string): Promise<CategoryAnalytics[]> {
  const snap = await getDocs(collection(db, "children", childId, "analyticsByCategory"));
  return snap.docs
    .map((item) => ({ id: item.id, ...item.data() } as CategoryAnalytics))
    .sort((a, b) => b.recommendationScore - a.recommendationScore);
}

export async function getRecommendations(childId: string): Promise<QuestRecommendation[]> {
  const snap = await getDocs(collection(db, "children", childId, "recommendations"));
  return snap.docs
    .map((item) => ({ id: item.id, ...item.data() } as QuestRecommendation))
    .sort((a, b) => b.score - a.score);
}

export async function getRecommendationForCategory(
  childId: string,
  category: string
): Promise<QuestRecommendation> {
  const snap = await getDoc(doc(db, "children", childId, "recommendations", category));
  if (snap.exists()) return { id: snap.id, ...snap.data() } as QuestRecommendation;

  const fallback = evaluateRecommendation({
    category,
    assignedTotal: 0,
    viewedTotal: 0,
    submittedTotal: 0,
    approvedTotal: 0,
    rejectedTotal: 0,
    claimedTotal: 0,
    reminderTotal: 0,
    completionRate: 0,
    approvalRate: 0,
    rejectionRate: 0,
    viewToSubmitRate: 0,
    reminderNeedRate: 0,
    avgSubmitHours: null,
    avgClaimHours: null,
    avgRewardXp: 0,
    avgRewardGold: 0,
    activeQuestCountToday: 0,
    sameCategoryAssignedLast7Days: 0,
    preferredLocalHour: null,
  });
  return {
    id: category,
    familyId: "",
    childId,
    targetCategory: category,
    generatedAt: null,
    ...fallback,
  };
}

export async function rebuildAnalyticsForChild(childId: string): Promise<void> {
  const rebuild = httpsCallable<{ childId: string }, { ok: boolean }>(
    functions,
    "rebuildChildAnalytics"
  );
  try {
    await rebuild({ childId });
  } catch (error) {
    const code = typeof error === "object" && error && "code" in error
      ? String(error.code)
      : "";
    if (code.includes("not-found") || code.includes("unavailable")) {
      throw new Error("Функция пересчета еще не развернута. Запустите DEPLOY_FIREBASE.bat и повторите попытку.");
    }
    throw error;
  }
}

export async function rebuildAnalyticsForChildLocally(childId: string): Promise<void> {
  const [eventSnap, childSnap] = await Promise.all([
    getDocs(query(collection(db, "children", childId, "questEvents"), orderBy("eventAt", "asc"))),
    getDoc(doc(db, "children", childId)),
  ]);
  const familyId = childSnap.data()?.familyId ?? "";
  const events = eventSnap.docs.map((item) => ({ id: item.id, ...item.data() } as QuestEvent));
  const byDate = new Map<string, QuestEvent[]>();
  const byCategory = new Map<string, QuestEvent[]>();

  for (const event of events) {
    const dateEvents = byDate.get(event.localDate) ?? [];
    dateEvents.push(event);
    byDate.set(event.localDate, dateEvents);

    const categoryEvents = byCategory.get(event.category || "custom") ?? [];
    categoryEvents.push(event);
    byCategory.set(event.category || "custom", categoryEvents);
  }

  const batch = writeBatch(db);
  for (const [date, dateEvents] of byDate) {
    const total = emptyStats();
    const categoryMap = new Map<string, MutableStats>();
    for (const event of dateEvents) {
      applyEvent(total, event);
      const categoryStats = categoryMap.get(event.category) ?? emptyStats();
      applyEvent(categoryStats, event);
      categoryMap.set(event.category, categoryStats);
    }

    const categoryStats: Record<string, DailyCategoryStats> = {};
    for (const [category, stats] of categoryMap) categoryStats[category] = dailyCategoryStats(stats);
    const activeQuestCount = Math.max(0, total.assigned - total.claimed - total.cancelled);
    batch.set(doc(db, "children", childId, "analyticsDaily", date), {
      date,
      familyId,
      childId,
      assignedCount: total.assigned,
      viewedCount: total.viewed,
      submittedCount: total.submitted,
      approvedCount: total.approved,
      rejectedCount: total.rejected,
      claimedCount: total.claimed,
      cancelledCount: total.cancelled,
      reminderCount: total.reminders,
      completionRate: rate(total.claimed, total.assigned),
      approvalRate: rate(total.approved, total.submitted),
      rejectionRate: rate(total.rejected, total.submitted),
      claimRate: rate(total.claimed, total.approved),
      avgSubmitHours: average(total.submitHours),
      avgClaimHours: average(total.claimHours),
      activeQuestCount,
      categoryStats,
      updatedAt: serverTimestamp(),
    });
  }

  const today = localDateParts().localDate;
  const last7Date = new Date();
  last7Date.setDate(last7Date.getDate() - 6);
  const last7Start = localDateParts(last7Date).localDate;
  const todayEvents = byDate.get(today) ?? [];
  const activeQuestCountToday = Math.max(
    0,
    todayEvents.filter((event) => event.eventType === "quest_created").length
      - todayEvents.filter((event) => ["quest_claimed", "quest_cancelled"].includes(event.eventType)).length
  );

  for (const [category, categoryEvents] of byCategory) {
    const stats = emptyStats();
    categoryEvents.forEach((event) => applyEvent(stats, event));
    const features: RecommendationFeatures = {
      category,
      assignedTotal: stats.assigned,
      viewedTotal: stats.viewed,
      submittedTotal: stats.submitted,
      approvedTotal: stats.approved,
      rejectedTotal: stats.rejected,
      claimedTotal: stats.claimed,
      reminderTotal: stats.reminders,
      completionRate: rate(stats.claimed, stats.assigned),
      approvalRate: rate(stats.approved, stats.submitted),
      rejectionRate: rate(stats.rejected, stats.submitted),
      viewToSubmitRate: rate(stats.submitted, stats.viewed),
      reminderNeedRate: rate(stats.reminders, stats.assigned),
      avgSubmitHours: average(stats.submitHours),
      avgClaimHours: average(stats.claimHours),
      avgRewardXp: average(stats.rewardXp) ?? 0,
      avgRewardGold: average(stats.rewardGold) ?? 0,
      activeQuestCountToday,
      sameCategoryAssignedLast7Days: categoryEvents.filter(
        (event) => event.eventType === "quest_created" && event.localDate >= last7Start
      ).length,
      preferredLocalHour: preferredHour(categoryEvents),
    };
    const recommendation = evaluateRecommendation(features);

    batch.set(doc(db, "children", childId, "analyticsByCategory", category), compact({
      category,
      familyId,
      childId,
      assignedTotal: stats.assigned,
      viewedTotal: stats.viewed,
      submittedTotal: stats.submitted,
      approvedTotal: stats.approved,
      rejectedTotal: stats.rejected,
      claimedTotal: stats.claimed,
      cancelledTotal: stats.cancelled,
      reminderTotal: stats.reminders,
      completionRate: features.completionRate,
      approvalRate: features.approvalRate,
      rejectionRate: features.rejectionRate,
      viewToSubmitRate: features.viewToSubmitRate,
      reminderNeedRate: features.reminderNeedRate,
      avgSubmitHours: features.avgSubmitHours,
      avgClaimHours: features.avgClaimHours,
      avgRewardXp: features.avgRewardXp,
      avgRewardGold: features.avgRewardGold,
      lastAssignedAt: stats.lastAssignedAt,
      lastClaimedAt: stats.lastClaimedAt,
      recommendationScore: recommendation.score,
      recommendationLabel: recommendation.label,
      recommendationReason: recommendation.reasons[0] ?? recommendation.risks[0] ?? "Недостаточно данных.",
      recommendationActions: recommendation.suggestedActions,
      updatedAt: serverTimestamp(),
    }));

    batch.set(doc(db, "children", childId, "recommendations", category), {
      familyId,
      childId,
      targetCategory: category,
      suggestedDifficulty: recommendation.suggestedDifficulty,
      suggestedRewardXp: recommendation.suggestedRewardXp,
      suggestedRewardGold: recommendation.suggestedRewardGold,
      score: recommendation.score,
      label: recommendation.label,
      reasons: recommendation.reasons,
      risks: recommendation.risks,
      suggestedActions: recommendation.suggestedActions,
      generatedAt: serverTimestamp(),
      engineVersion: recommendation.engineVersion,
    });
  }

  await batch.commit();
}

function preferredHour(events: QuestEvent[]): number | null {
  const hours = events
    .filter((event) => event.eventType === "quest_submitted")
    .map((event) => event.localHour)
    .filter((hour) => Number.isInteger(hour));
  if (hours.length === 0) return null;
  const counts = new Map<number, number>();
  hours.forEach((hour) => counts.set(hour, (counts.get(hour) ?? 0) + 1));
  return [...counts.entries()].sort((a, b) => b[1] - a[1])[0][0];
}
