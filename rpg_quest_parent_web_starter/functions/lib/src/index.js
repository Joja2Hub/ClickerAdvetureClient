"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.claimQuestReward = exports.createChildLogin = exports.rejectQuest = exports.approveQuest = void 0;
const app_1 = require("firebase-admin/app");
const auth_1 = require("firebase-admin/auth");
const firestore_1 = require("firebase-admin/firestore");
const https_1 = require("firebase-functions/v2/https");
(0, app_1.initializeApp)();
const db = (0, firestore_1.getFirestore)();
async function assertParentForChild(uid, childId) {
    if (!uid)
        throw new https_1.HttpsError("unauthenticated", "Нужна авторизация.");
    const userSnap = await db.doc(`users/${uid}`).get();
    if (!userSnap.exists || userSnap.data()?.role !== "parent") {
        throw new https_1.HttpsError("permission-denied", "Действие доступно только родителю.");
    }
    const childSnap = await db.doc(`children/${childId}`).get();
    if (!childSnap.exists)
        throw new https_1.HttpsError("not-found", "Ребёнок не найден.");
    if (userSnap.data()?.familyId !== childSnap.data()?.familyId) {
        throw new https_1.HttpsError("permission-denied", "Ребёнок не принадлежит вашей семье.");
    }
    return { user: userSnap.data(), child: childSnap.data() };
}
function todayId(date = new Date()) {
    return date.toISOString().slice(0, 10);
}
exports.approveQuest = (0, https_1.onCall)({ region: "europe-west1" }, async (request) => {
    const { childId, questId } = request.data;
    if (!childId || !questId)
        throw new https_1.HttpsError("invalid-argument", "childId и questId обязательны.");
    await assertParentForChild(request.auth?.uid, childId);
    const questRef = db.doc(`children/${childId}/quests/${questId}`);
    const rewardRef = db.doc(`children/${childId}/rewardLedger/quest_${questId}`);
    const analyticsRef = db.doc(`children/${childId}/analyticsDaily/${todayId()}`);
    await db.runTransaction(async (tx) => {
        const questSnap = await tx.get(questRef);
        if (!questSnap.exists)
            throw new https_1.HttpsError("not-found", "Квест не найден.");
        const quest = questSnap.data();
        if (quest.status !== "submitted") {
            throw new https_1.HttpsError("failed-precondition", "Одобрить можно только квест на проверке.");
        }
        const rewardSnap = await tx.get(rewardRef);
        if (rewardSnap.exists) {
            throw new https_1.HttpsError("already-exists", "Награда для этого квеста уже создана.");
        }
        tx.update(questRef, {
            status: "approved",
            approvedAt: firestore_1.Timestamp.now(),
            parentComment: "",
        });
        tx.set(rewardRef, {
            sourceType: "realQuest",
            sourceId: questId,
            xp: quest.rewardXp ?? 0,
            gold: quest.rewardGold ?? 0,
            items: quest.rewardItems ?? [],
            claimed: false,
            createdAt: firestore_1.Timestamp.now(),
            claimedAt: null,
        });
        tx.set(analyticsRef, {
            questsApproved: firestore_1.FieldValue.increment(1),
        }, { merge: true });
    });
    return { ok: true };
});
exports.rejectQuest = (0, https_1.onCall)({ region: "europe-west1" }, async (request) => {
    const { childId, questId, parentComment } = request.data;
    if (!childId || !questId)
        throw new https_1.HttpsError("invalid-argument", "childId и questId обязательны.");
    await assertParentForChild(request.auth?.uid, childId);
    const questRef = db.doc(`children/${childId}/quests/${questId}`);
    await db.runTransaction(async (tx) => {
        const questSnap = await tx.get(questRef);
        if (!questSnap.exists)
            throw new https_1.HttpsError("not-found", "Квест не найден.");
        if (questSnap.data()?.status !== "submitted") {
            throw new https_1.HttpsError("failed-precondition", "Отклонить можно только квест на проверке.");
        }
        tx.update(questRef, {
            status: "rejected",
            rejectedAt: firestore_1.Timestamp.now(),
            parentComment: parentComment ?? "Попробуй ещё раз.",
        });
    });
    return { ok: true };
});
exports.createChildLogin = (0, https_1.onCall)({ region: "europe-west1" }, async (request) => {
    const { childId, email, password, displayName } = request.data;
    if (!childId || !email || !password) {
        throw new https_1.HttpsError("invalid-argument", "childId, email и password обязательны.");
    }
    if (password.length < 6) {
        throw new https_1.HttpsError("invalid-argument", "Пароль должен быть минимум 6 символов.");
    }
    const { child } = await assertParentForChild(request.auth?.uid, childId);
    const auth = (0, auth_1.getAuth)();
    let childUser;
    try {
        childUser = await auth.getUserByEmail(email);
        const existingProfile = await db.doc(`users/${childUser.uid}`).get();
        const existingChildId = existingProfile.data()?.childId;
        if (existingChildId && existingChildId !== childId) {
            throw new https_1.HttpsError("already-exists", "Этот email уже привязан к другому ребёнку.");
        }
        await auth.updateUser(childUser.uid, {
            password,
            displayName: displayName || child.displayName || "Юный герой",
        });
    }
    catch (error) {
        if (error instanceof https_1.HttpsError)
            throw error;
        childUser = await auth.createUser({
            email,
            password,
            displayName: displayName || child.displayName || "Юный герой",
        });
    }
    await db.doc(`users/${childUser.uid}`).set({
        uid: childUser.uid,
        role: "child",
        familyId: child.familyId,
        childId,
        displayName: displayName || child.displayName || "Юный герой",
        email,
        updatedAt: firestore_1.Timestamp.now(),
    }, { merge: true });
    return { ok: true, uid: childUser.uid };
});
exports.claimQuestReward = (0, https_1.onCall)({ region: "europe-west1" }, async (request) => {
    const { childId, questId } = request.data;
    if (!childId || !questId)
        throw new https_1.HttpsError("invalid-argument", "childId и questId обязательны.");
    if (!request.auth?.uid)
        throw new https_1.HttpsError("unauthenticated", "Нужна авторизация.");
    const userSnap = await db.doc(`users/${request.auth.uid}`).get();
    const user = userSnap.data();
    if (!user || user.role !== "child" || user.childId !== childId) {
        throw new https_1.HttpsError("permission-denied", "Награду может забрать только ребёнок-владелец профиля.");
    }
    const questRef = db.doc(`children/${childId}/quests/${questId}`);
    const rewardRef = db.doc(`children/${childId}/rewardLedger/quest_${questId}`);
    const childRef = db.doc(`children/${childId}`);
    const gameStateRef = db.doc(`children/${childId}/gameState/main`);
    const analyticsRef = db.doc(`children/${childId}/analyticsDaily/${todayId()}`);
    await db.runTransaction(async (tx) => {
        const [questSnap, rewardSnap] = await Promise.all([tx.get(questRef), tx.get(rewardRef)]);
        if (!questSnap.exists || !rewardSnap.exists)
            throw new https_1.HttpsError("not-found", "Награда не найдена.");
        const quest = questSnap.data();
        const reward = rewardSnap.data();
        if (quest.status !== "approved")
            throw new https_1.HttpsError("failed-precondition", "Квест ещё не одобрен.");
        if (reward.claimed)
            throw new https_1.HttpsError("already-exists", "Награда уже получена.");
        const xp = reward.xp ?? 0;
        const gold = reward.gold ?? 0;
        tx.update(rewardRef, { claimed: true, claimedAt: firestore_1.Timestamp.now() });
        tx.update(questRef, { status: "claimed", claimedAt: firestore_1.Timestamp.now() });
        tx.set(childRef, { xp: firestore_1.FieldValue.increment(xp), gold: firestore_1.FieldValue.increment(gold), updatedAt: firestore_1.Timestamp.now() }, { merge: true });
        tx.set(gameStateRef, { xp: firestore_1.FieldValue.increment(xp), gold: firestore_1.FieldValue.increment(gold), lastActiveAt: firestore_1.Timestamp.now() }, { merge: true });
        tx.set(analyticsRef, { xpEarned: firestore_1.FieldValue.increment(xp), goldEarned: firestore_1.FieldValue.increment(gold) }, { merge: true });
    });
    return { ok: true };
});
//# sourceMappingURL=index.js.map