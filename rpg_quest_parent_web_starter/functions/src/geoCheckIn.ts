import { FieldValue, getFirestore, type Firestore } from "firebase-admin/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";
import { evaluateGeoVisit, GeoPolicyError } from "./geoPolicy.js";

function id(value: unknown): string {
  if (typeof value !== "string" || !value.trim() || value.length > 128
    || value.includes("/") || value === "." || value === "..")
    throw new HttpsError("invalid-argument", "Некорректный ID ребёнка или задания.");
  return value;
}

// Exported separately for deterministic local tests; the callable always supplies server time.
export async function registerGeoVisit(db: Firestore, uid: string | undefined, data: unknown,
  serverNow = Math.floor(Date.now() / 1000)) {
  if (!uid) throw new HttpsError("unauthenticated", "Нужна авторизация.");
  if (!data || typeof data !== "object" || Array.isArray(data))
    throw new HttpsError("invalid-argument", "Нужны данные посещения.");
  const input = data as Record<string, unknown>;
  const childId = id(input.childId);
  const questId = id(input.questId);
  const questRef = db.doc(`children/${childId}/quests/${questId}`);
  return db.runTransaction(async tx => {
    const [userSnap, childSnap, questSnap] = await Promise.all([
      tx.get(db.doc(`users/${uid}`)), tx.get(db.doc(`children/${childId}`)), tx.get(questRef),
    ]);
    const user = userSnap.data();
    const child = childSnap.data();
    if (!user || user.role !== "child" || user.childId !== childId || !child
      || typeof user.familyId !== "string" || !user.familyId || user.familyId !== child.familyId)
      throw new HttpsError("permission-denied", "Отметить посещение может только ребёнок-владелец задания.");
    if (!questSnap.exists) throw new HttpsError("not-found", "Квест не найден.");
    const quest = questSnap.data()!;
    let visit;
    try { visit = evaluateGeoVisit(quest.geo, input, serverNow); }
    catch (error) {
      if (error instanceof GeoPolicyError) throw new HttpsError(error.code, error.message);
      throw error;
    }
    const progress = quest.geoProgress;
    if (progress != null && (typeof progress !== "object" || Array.isArray(progress) || progress.definitionHash !== visit.definitionHash
      || !Number.isInteger(progress.visitCount) || progress.visitCount < 0 || progress.visitCount > visit.goal))
      throw new HttpsError("failed-precondition", "Прогресс не соответствует условиям. Попросите родителя проверить задание.");
    const count: number = progress?.visitCount ?? 0;
    const visitRef = questRef.collection("geoVisits").doc(`${visit.definitionHash}_${visit.day}`);
    const previous = await tx.get(visitRef);
    if (previous.exists) {
      if (count < 1) throw new HttpsError("failed-precondition", "Некорректный прогресс задания.");
      return { visitCount: count, requiredVisits: visit.goal, alreadyVisitedToday: true, status: quest.status };
    }
    if (quest.status !== "assigned" && quest.status !== "rejected")
      throw new HttpsError("failed-precondition", "Задание больше не принимает посещения.");
    if (count >= visit.goal) throw new HttpsError("failed-precondition", "Цель уже достигнута.");
    const visitCount = count + 1;
    const status = visitCount === visit.goal ? "submitted" : quest.status;
    tx.create(visitRef, {
      day: visit.day, definitionHash: visit.definitionHash,
      observedAtUnixSeconds: visit.observedAt, receivedAt: FieldValue.serverTimestamp(),
    });
    tx.update(questRef, {
      geoProgress: { definitionHash: visit.definitionHash, visitCount, lastVisitDay: visit.day },
      ...(status === "submitted" ? { status, submittedAt: FieldValue.serverTimestamp() } : {}),
    });
    return { visitCount, requiredVisits: visit.goal, alreadyVisitedToday: false, status };
  });
}

export const checkInGeoQuest = onCall({ region: "europe-west1" }, request =>
  registerGeoVisit(getFirestore(), request.auth?.uid, request.data));
