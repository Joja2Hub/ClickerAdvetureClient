"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkInGeoQuest = void 0;
exports.registerGeoVisit = registerGeoVisit;
const firestore_1 = require("firebase-admin/firestore");
const https_1 = require("firebase-functions/v2/https");
const geoPolicy_js_1 = require("./geoPolicy.js");
function id(value) {
    if (typeof value !== "string" || !value.trim() || value.length > 128
        || value.includes("/") || value === "." || value === "..")
        throw new https_1.HttpsError("invalid-argument", "Некорректный ID ребёнка или задания.");
    return value;
}
// Exported separately for deterministic local tests; the callable always supplies server time.
async function registerGeoVisit(db, uid, data, serverNow = Math.floor(Date.now() / 1000)) {
    if (!uid)
        throw new https_1.HttpsError("unauthenticated", "Нужна авторизация.");
    if (!data || typeof data !== "object" || Array.isArray(data))
        throw new https_1.HttpsError("invalid-argument", "Нужны данные посещения.");
    const input = data;
    const childId = id(input.childId);
    const questId = id(input.questId);
    const questRef = db.doc(`children/${childId}/quests/${questId}`);
    return db.runTransaction(async (tx) => {
        const [userSnap, childSnap, questSnap] = await Promise.all([
            tx.get(db.doc(`users/${uid}`)), tx.get(db.doc(`children/${childId}`)), tx.get(questRef),
        ]);
        const user = userSnap.data();
        const child = childSnap.data();
        if (!user || user.role !== "child" || user.childId !== childId || !child
            || typeof user.familyId !== "string" || !user.familyId || user.familyId !== child.familyId)
            throw new https_1.HttpsError("permission-denied", "Отметить посещение может только ребёнок-владелец задания.");
        if (!questSnap.exists)
            throw new https_1.HttpsError("not-found", "Квест не найден.");
        const quest = questSnap.data();
        let visit;
        try {
            visit = (0, geoPolicy_js_1.evaluateGeoVisit)(quest.geo, input, serverNow);
        }
        catch (error) {
            if (error instanceof geoPolicy_js_1.GeoPolicyError)
                throw new https_1.HttpsError(error.code, error.message);
            throw error;
        }
        const progress = quest.geoProgress;
        if (progress != null && (typeof progress !== "object" || Array.isArray(progress) || progress.definitionHash !== visit.definitionHash
            || !Number.isInteger(progress.visitCount) || progress.visitCount < 0 || progress.visitCount > visit.goal))
            throw new https_1.HttpsError("failed-precondition", "Прогресс не соответствует условиям. Попросите родителя проверить задание.");
        const count = progress?.visitCount ?? 0;
        const visitRef = questRef.collection("geoVisits").doc(`${visit.definitionHash}_${visit.day}`);
        const previous = await tx.get(visitRef);
        if (previous.exists) {
            if (count < 1)
                throw new https_1.HttpsError("failed-precondition", "Некорректный прогресс задания.");
            return { visitCount: count, requiredVisits: visit.goal, alreadyVisitedToday: true, status: quest.status };
        }
        if (quest.status !== "assigned" && quest.status !== "rejected")
            throw new https_1.HttpsError("failed-precondition", "Задание больше не принимает посещения.");
        if (count >= visit.goal)
            throw new https_1.HttpsError("failed-precondition", "Цель уже достигнута.");
        const visitCount = count + 1;
        const status = visitCount === visit.goal ? "submitted" : quest.status;
        tx.create(visitRef, {
            day: visit.day, definitionHash: visit.definitionHash,
            observedAtUnixSeconds: visit.observedAt, receivedAt: firestore_1.FieldValue.serverTimestamp(),
        });
        tx.update(questRef, {
            geoProgress: { definitionHash: visit.definitionHash, visitCount, lastVisitDay: visit.day },
            ...(status === "submitted" ? { status, submittedAt: firestore_1.FieldValue.serverTimestamp() } : {}),
        });
        return { visitCount, requiredVisits: visit.goal, alreadyVisitedToday: false, status };
    });
}
exports.checkInGeoQuest = (0, https_1.onCall)({ region: "europe-west1" }, request => registerGeoVisit((0, firestore_1.getFirestore)(), request.auth?.uid, request.data));
//# sourceMappingURL=geoCheckIn.js.map