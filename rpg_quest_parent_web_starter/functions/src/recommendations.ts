import { getApps, initializeApp } from "firebase-admin/app";
import { Timestamp, getFirestore } from "firebase-admin/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";

if (getApps().length === 0) initializeApp();
const db = getFirestore();
export const ENGINE_VERSION = "expert-v1";

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

type RuleResult = { delta: number; reason?: string; risk?: string; action?: string };

export function evaluateRecommendation(features: RecommendationFeatures) {
  const results: RuleResult[] = [];
  if (features.completionRate >= 0.75 && features.rejectionRate <= 0.15) {
    results.push({ delta: 25, reason: "Ребенок стабильно выполняет задания этой категории." });
  }
  if (features.avgSubmitHours !== null && features.avgSubmitHours <= 24) {
    results.push({ delta: 15, reason: "Похожие задания обычно выполняются быстро." });
  }
  if (features.rejectionRate >= 0.35) {
    results.push({ delta: -30, risk: "Задания этой категории часто требуют доработки.", action: "Упростить формулировку или уменьшить объем." });
  }
  if (features.viewedTotal >= 3 && features.viewToSubmitRate < 0.4) {
    results.push({ delta: -20, risk: "Ребенок открывает задания, но редко отправляет их на проверку.", action: "Изменить награду или сделать задание короче." });
  }
  if (features.activeQuestCountToday >= 5) {
    results.push({ delta: -25, risk: "На сегодня уже назначено много активных заданий.", action: "Перенести задание на другой день." });
  }
  if (features.sameCategoryAssignedLast7Days >= 5 && features.completionRate < 0.6) {
    results.push({ delta: -20, risk: "Возможна усталость от однотипных заданий.", action: "Сменить категорию." });
  }
  if (features.assignedTotal < 3) {
    results.push({ delta: 0, reason: "По категории пока мало данных.", action: "Использовать пробное задание низкой сложности." });
  }
  if (features.completionRate >= 0.7 && features.avgRewardGold <= 50) {
    results.push({ delta: 10, reason: "Категория выполняется успешно без завышенной награды." });
  }

  const score = Math.max(0, Math.min(100, 50 + results.reduce((sum, item) => sum + item.delta, 0)));
  const label = score >= 70 ? "recommended" : score >= 40 ? "neutral" : "not_recommended";
  const suggestedDifficulty = features.assignedTotal < 3 || score < 40 ? "easy" : score >= 85 ? "hard" : "normal";
  const factor = label === "not_recommended" ? 0.9 : label === "recommended" ? 1 : 0.95;
  return {
    score,
    label,
    suggestedDifficulty,
    suggestedRewardXp: Math.max(20, Math.round(((features.avgRewardXp || 50) * factor) / 5) * 5),
    suggestedRewardGold: Math.max(10, Math.round(((features.avgRewardGold || 25) * factor) / 5) * 5),
    reasons: results.flatMap((item) => item.reason ? [item.reason] : []),
    risks: results.flatMap((item) => item.risk ? [item.risk] : []),
    suggestedActions: results.flatMap((item) => item.action ? [item.action] : []),
    engineVersion: ENGINE_VERSION,
  };
}

async function assertParentForChild(uid: string | undefined, childId: string) {
  if (!uid) throw new HttpsError("unauthenticated", "Нужна авторизация.");
  const [userSnap, childSnap] = await Promise.all([
    db.doc(`users/${uid}`).get(),
    db.doc(`children/${childId}`).get(),
  ]);
  if (!userSnap.exists || userSnap.data()?.role !== "parent") {
    throw new HttpsError("permission-denied", "Действие доступно только родителю.");
  }
  if (!childSnap.exists || userSnap.data()?.familyId !== childSnap.data()?.familyId) {
    throw new HttpsError("permission-denied", "Ребенок не принадлежит вашей семье.");
  }
  return childSnap.data()!;
}

export const generateQuestRecommendations = onCall({ region: "europe-west1" }, async (request) => {
  const childId = String(request.data?.childId ?? "");
  if (!childId) throw new HttpsError("invalid-argument", "childId обязателен.");
  const child = await assertParentForChild(request.auth?.uid, childId);
  const analyticsSnap = await db.collection(`children/${childId}/analyticsByCategory`).get();
  const batch = db.batch();
  const recommendations = analyticsSnap.docs.map((item) => {
    const analytics = item.data() as RecommendationFeatures;
    const result = evaluateRecommendation(analytics);
    const document = {
      familyId: child.familyId,
      childId,
      targetCategory: item.id,
      ...result,
      generatedAt: Timestamp.now(),
    };
    batch.set(db.doc(`children/${childId}/recommendations/${item.id}`), document);
    return { id: item.id, ...document };
  });
  await batch.commit();
  return recommendations;
});
