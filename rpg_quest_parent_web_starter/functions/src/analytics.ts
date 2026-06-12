import { getApps, initializeApp } from "firebase-admin/app";
import { FieldValue, Timestamp, getFirestore } from "firebase-admin/firestore";
import { onDocumentCreated } from "firebase-functions/v2/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";
import { evaluateRecommendation, RecommendationFeatures } from "./recommendations.js";

if (getApps().length === 0) initializeApp();
const db = getFirestore();

type EventData = {
  familyId?: string;
  childId?: string;
  category?: string;
  eventType?: string;
  localDate?: string;
  localHour?: number;
  rewardXp?: number;
  rewardGold?: number;
  timeFromAssignedHours?: number;
  timeFromSubmittedHours?: number;
  eventAt?: Timestamp;
};

type Stats = {
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

const emptyStats = (): Stats => ({
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
const rate = (numerator: number, denominator: number) => denominator > 0 ? numerator / denominator : 0;
const average = (values: number[]) => values.length > 0
  ? values.reduce((sum, value) => sum + value, 0) / values.length
  : null;

function apply(stats: Stats, event: EventData) {
  switch (event.eventType) {
    case "quest_created":
      stats.assigned++;
      stats.rewardXp.push(event.rewardXp ?? 0);
      stats.rewardGold.push(event.rewardGold ?? 0);
      stats.lastAssignedAt = event.eventAt ?? stats.lastAssignedAt;
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
      stats.lastClaimedAt = event.eventAt ?? stats.lastClaimedAt;
      break;
    case "quest_cancelled":
      stats.cancelled++;
      break;
    case "reminder_sent":
      stats.reminders++;
      break;
  }
}

export async function rebuildChildAnalyticsData(childId: string) {
  const [eventsSnap, childSnap] = await Promise.all([
    db.collection(`children/${childId}/questEvents`).orderBy("eventAt", "asc").get(),
    db.doc(`children/${childId}`).get(),
  ]);
  if (!childSnap.exists) throw new HttpsError("not-found", "Ребенок не найден.");
  const familyId = childSnap.data()?.familyId ?? "";
  const events = eventsSnap.docs.map((item) => item.data() as EventData);
  const byDate = new Map<string, EventData[]>();
  const byCategory = new Map<string, EventData[]>();
  for (const event of events) {
    const date = event.localDate ?? "unknown";
    const category = event.category ?? "custom";
    byDate.set(date, [...(byDate.get(date) ?? []), event]);
    byCategory.set(category, [...(byCategory.get(category) ?? []), event]);
  }

  const batch = db.batch();
  for (const [date, dateEvents] of byDate) {
    const total = emptyStats();
    const categoryStats: Record<string, unknown> = {};
    const localCategories = new Map<string, Stats>();
    dateEvents.forEach((event) => {
      apply(total, event);
      const stats = localCategories.get(event.category ?? "custom") ?? emptyStats();
      apply(stats, event);
      localCategories.set(event.category ?? "custom", stats);
    });
    for (const [category, stats] of localCategories) {
      categoryStats[category] = {
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
    batch.set(db.doc(`children/${childId}/analyticsDaily/${date}`), {
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
      activeQuestCount: Math.max(0, total.assigned - total.claimed - total.cancelled),
      categoryStats,
      updatedAt: FieldValue.serverTimestamp(),
    });
  }

  const today = new Date().toISOString().slice(0, 10);
  const last7 = new Date();
  last7.setUTCDate(last7.getUTCDate() - 6);
  const last7Start = last7.toISOString().slice(0, 10);
  const todayEvents = byDate.get(today) ?? [];
  const activeQuestCountToday = Math.max(
    0,
    todayEvents.filter((event) => event.eventType === "quest_created").length
      - todayEvents.filter((event) => ["quest_claimed", "quest_cancelled"].includes(event.eventType ?? "")).length
  );

  for (const [category, categoryEvents] of byCategory) {
    const stats = emptyStats();
    categoryEvents.forEach((event) => apply(stats, event));
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
        (event) => event.eventType === "quest_created" && (event.localDate ?? "") >= last7Start
      ).length,
      preferredLocalHour: null,
    };
    const recommendation = evaluateRecommendation(features);
    batch.set(db.doc(`children/${childId}/analyticsByCategory/${category}`), {
      ...features,
      familyId,
      childId,
      cancelledTotal: stats.cancelled,
      lastAssignedAt: stats.lastAssignedAt ?? null,
      lastClaimedAt: stats.lastClaimedAt ?? null,
      recommendationScore: recommendation.score,
      recommendationLabel: recommendation.label,
      recommendationReason: recommendation.reasons[0] ?? recommendation.risks[0] ?? "Недостаточно данных.",
      recommendationActions: recommendation.suggestedActions,
      updatedAt: FieldValue.serverTimestamp(),
    });
    batch.set(db.doc(`children/${childId}/recommendations/${category}`), {
      familyId,
      childId,
      targetCategory: category,
      ...recommendation,
      generatedAt: FieldValue.serverTimestamp(),
    });
  }
  await batch.commit();
}

export const onQuestEventCreated = onDocumentCreated(
  { document: "children/{childId}/questEvents/{eventId}", region: "europe-west1" },
  async (event) => rebuildChildAnalyticsData(event.params.childId)
);

export const rebuildChildAnalytics = onCall({ region: "europe-west1" }, async (request) => {
  const childId = String(request.data?.childId ?? "");
  if (!request.auth?.uid) throw new HttpsError("unauthenticated", "Нужна авторизация.");
  const [userSnap, childSnap] = await Promise.all([
    db.doc(`users/${request.auth.uid}`).get(),
    db.doc(`children/${childId}`).get(),
  ]);
  if (!childId || userSnap.data()?.role !== "parent" || userSnap.data()?.familyId !== childSnap.data()?.familyId) {
    throw new HttpsError("permission-denied", "Нет доступа к аналитике ребенка.");
  }
  await rebuildChildAnalyticsData(childId);
  return { ok: true };
});
