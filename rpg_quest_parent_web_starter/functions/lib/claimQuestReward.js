"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.claimReward = claimReward;
const firestore_1 = require("firebase-admin/firestore");
const https_1 = require("firebase-functions/v2/https");
function validId(value) {
    return typeof value === "string" && value.trim().length > 0
        && !value.includes("/") && value !== "." && value !== "..";
}
async function claimReward(db, uid, input) {
    if (!uid)
        throw new https_1.HttpsError("unauthenticated", "Нужна авторизация.");
    const { childId, questId } = (input ?? {});
    if (!validId(childId) || !validId(questId)) {
        throw new https_1.HttpsError("invalid-argument", "Нужны корректные childId и questId.");
    }
    const userRef = db.doc(`users/${uid}`);
    const childRef = db.doc(`children/${childId}`);
    const questRef = childRef.collection("quests").doc(questId);
    const rewardRef = childRef.collection("rewardLedger").doc(`quest_${questId}`);
    const gameStateRef = childRef.collection("gameState").doc("main");
    const now = firestore_1.Timestamp.now();
    const analyticsRef = childRef.collection("analyticsDaily").doc(now.toDate().toISOString().slice(0, 10));
    await db.runTransaction(async (tx) => {
        const [userSnap, childSnap, questSnap, rewardSnap] = await Promise.all([
            tx.get(userRef), tx.get(childRef), tx.get(questRef), tx.get(rewardRef),
        ]);
        const user = userSnap.data();
        const child = childSnap.data();
        if (!user || user.role !== "child" || user.childId !== childId || !child
            || typeof user.familyId !== "string" || !user.familyId || user.familyId !== child.familyId) {
            throw new https_1.HttpsError("permission-denied", "Награду может забрать только ребёнок-владелец профиля.");
        }
        const quest = questSnap.data();
        if (!quest)
            throw new https_1.HttpsError("not-found", "Квест не найден.");
        const reward = rewardSnap.data();
        if (quest.status === "claimed" || quest.claimedAt != null || reward?.claimed === true) {
            throw new https_1.HttpsError("already-exists", "Награда уже получена.");
        }
        if (quest.status !== "approved")
            throw new https_1.HttpsError("failed-precondition", "Квест ещё не одобрен.");
        let xp;
        let gold;
        if (reward) {
            xp = reward.xp ?? 0;
            gold = reward.gold ?? 0;
        }
        else {
            // The old parent dashboard approved ordinary quests directly and left no ledger entry.
            if (quest.geo != null || !(quest.approvedAt instanceof firestore_1.Timestamp)) {
                throw new https_1.HttpsError("failed-precondition", "Для задания отсутствует подтверждённая награда.");
            }
            xp = quest.rewardXp ?? 0;
            gold = quest.rewardGold ?? 0;
            if (!Number.isSafeInteger(xp) || xp < 0 || !Number.isSafeInteger(gold) || gold < 0) {
                throw new https_1.HttpsError("failed-precondition", "В задании указана некорректная награда.");
            }
            tx.create(rewardRef, {
                sourceType: "realQuest", sourceId: questId, xp, gold, items: quest.rewardItems ?? [],
                claimed: true, createdAt: now, claimedAt: now,
            });
        }
        if (reward)
            tx.update(rewardRef, { claimed: true, claimedAt: now });
        tx.update(questRef, { status: "claimed", claimedAt: now });
        tx.set(childRef, { xp: firestore_1.FieldValue.increment(xp), gold: firestore_1.FieldValue.increment(gold), updatedAt: now }, { merge: true });
        tx.set(gameStateRef, { xp: firestore_1.FieldValue.increment(xp), gold: firestore_1.FieldValue.increment(gold), lastActiveAt: now }, { merge: true });
        tx.set(analyticsRef, { xpEarned: firestore_1.FieldValue.increment(xp), goldEarned: firestore_1.FieldValue.increment(gold) }, { merge: true });
    });
    return { ok: true };
}
//# sourceMappingURL=claimQuestReward.js.map