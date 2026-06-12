import { getApps, initializeApp } from "firebase-admin/app";
import { getAuth } from "firebase-admin/auth";
import { getFirestore, FieldValue, Timestamp } from "firebase-admin/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";

export { onQuestEventCreated, rebuildChildAnalytics } from "./analytics.js";
export { generateQuestRecommendations } from "./recommendations.js";

if (getApps().length === 0) initializeApp();
const db = getFirestore();

async function assertParentForChild(uid: string | undefined, childId: string) {
  if (!uid) throw new HttpsError("unauthenticated", "Нужна авторизация.");

  const userSnap = await db.doc(`users/${uid}`).get();
  if (!userSnap.exists || userSnap.data()?.role !== "parent") {
    throw new HttpsError("permission-denied", "Действие доступно только родителю.");
  }

  const childSnap = await db.doc(`children/${childId}`).get();
  if (!childSnap.exists) throw new HttpsError("not-found", "Ребёнок не найден.");
  const child = childSnap.data();
  if (!child) throw new HttpsError("not-found", "Ребёнок не найден.");

  if (userSnap.data()?.familyId !== child.familyId) {
    throw new HttpsError("permission-denied", "Ребёнок не принадлежит вашей семье.");
  }

  return { user: userSnap.data(), child };
}

function todayId(date = new Date()): string {
  return date.toISOString().slice(0, 10);
}

function authErrorToHttps(error: unknown): HttpsError {
  const code = typeof error === "object" && error && "code" in error ? String(error.code) : "";
  const message = error instanceof Error ? error.message : "Ошибка Firebase Auth.";

  if (code === "auth/email-already-exists") {
    return new HttpsError("already-exists", "Этот email уже занят другим аккаунтом.");
  }
  if (code === "auth/invalid-email") {
    return new HttpsError("invalid-argument", "Некорректный email ребёнка.");
  }
  if (code === "auth/invalid-password") {
    return new HttpsError("invalid-argument", "Некорректный пароль ребёнка.");
  }

  return new HttpsError("internal", message);
}

export const approveQuest = onCall({ region: "europe-west1" }, async (request) => {
  const { childId, questId } = request.data as { childId?: string; questId?: string };
  if (!childId || !questId) throw new HttpsError("invalid-argument", "childId и questId обязательны.");

  await assertParentForChild(request.auth?.uid, childId);

  const questRef = db.doc(`children/${childId}/quests/${questId}`);
  const rewardRef = db.doc(`children/${childId}/rewardLedger/quest_${questId}`);
  const analyticsRef = db.doc(`children/${childId}/analyticsDaily/${todayId()}`);

  await db.runTransaction(async (tx) => {
    const questSnap = await tx.get(questRef);
    if (!questSnap.exists) throw new HttpsError("not-found", "Квест не найден.");
    const quest = questSnap.data()!;
    if (quest.status !== "submitted") {
      throw new HttpsError("failed-precondition", "Одобрить можно только квест на проверке.");
    }

    const rewardSnap = await tx.get(rewardRef);
    if (rewardSnap.exists) {
      throw new HttpsError("already-exists", "Награда для этого квеста уже создана.");
    }

    tx.update(questRef, {
      status: "approved",
      approvedAt: Timestamp.now(),
      parentComment: "",
    });

    tx.set(rewardRef, {
      sourceType: "realQuest",
      sourceId: questId,
      xp: quest.rewardXp ?? 0,
      gold: quest.rewardGold ?? 0,
      items: quest.rewardItems ?? [],
      claimed: false,
      createdAt: Timestamp.now(),
      claimedAt: null,
    });

    tx.set(
      analyticsRef,
      {
        questsApproved: FieldValue.increment(1),
      },
      { merge: true }
    );
  });

  return { ok: true };
});

export const rejectQuest = onCall({ region: "europe-west1" }, async (request) => {
  const { childId, questId, parentComment } = request.data as {
    childId?: string;
    questId?: string;
    parentComment?: string;
  };
  if (!childId || !questId) throw new HttpsError("invalid-argument", "childId и questId обязательны.");

  await assertParentForChild(request.auth?.uid, childId);

  const questRef = db.doc(`children/${childId}/quests/${questId}`);
  await db.runTransaction(async (tx) => {
    const questSnap = await tx.get(questRef);
    if (!questSnap.exists) throw new HttpsError("not-found", "Квест не найден.");
    if (questSnap.data()?.status !== "submitted") {
      throw new HttpsError("failed-precondition", "Отклонить можно только квест на проверке.");
    }
    tx.update(questRef, {
      status: "rejected",
      rejectedAt: Timestamp.now(),
      parentComment: parentComment ?? "Попробуй ещё раз.",
    });
  });

  return { ok: true };
});

export const createChildLogin = onCall({ region: "europe-west1" }, async (request) => {
  const { childId, email, password, displayName } = request.data as {
    childId?: string;
    email?: string;
    password?: string;
    displayName?: string;
  };
  if (!childId || !email || !password) {
    throw new HttpsError("invalid-argument", "childId, email и password обязательны.");
  }
  if (password.length < 6) {
    throw new HttpsError("invalid-argument", "Пароль должен быть минимум 6 символов.");
  }

  const { child } = await assertParentForChild(request.auth?.uid, childId);
  const auth = getAuth();
  let childUser;

  try {
    childUser = await auth.getUserByEmail(email);
    const existingProfile = await db.doc(`users/${childUser.uid}`).get();
    const existing = existingProfile.data();
    if (existing && (existing.role !== "child" || existing.childId !== childId)) {
      throw new HttpsError("already-exists", "Этот email уже привязан к другому профилю.");
    }

    try {
      await auth.updateUser(childUser.uid, {
        password,
        displayName: displayName || child.displayName || "Юный герой",
      });
    } catch (error) {
      throw authErrorToHttps(error);
    }
  } catch (error) {
    if (error instanceof HttpsError) throw error;

    const code = typeof error === "object" && error && "code" in error ? String(error.code) : "";
    if (code !== "auth/user-not-found") {
      throw authErrorToHttps(error);
    }

    try {
      childUser = await auth.createUser({
        email,
        password,
        displayName: displayName || child.displayName || "Юный герой",
      });
    } catch (createError) {
      throw authErrorToHttps(createError);
    }
  }

  await db.doc(`users/${childUser.uid}`).set(
    {
      uid: childUser.uid,
      role: "child",
      familyId: child.familyId,
      childId,
      displayName: displayName || child.displayName || "Юный герой",
      email,
      updatedAt: Timestamp.now(),
    },
    { merge: true }
  );

  return { ok: true, uid: childUser.uid };
});

export const claimQuestReward = onCall({ region: "europe-west1" }, async (request) => {
  const { childId, questId } = request.data as { childId?: string; questId?: string };
  if (!childId || !questId) throw new HttpsError("invalid-argument", "childId и questId обязательны.");
  if (!request.auth?.uid) throw new HttpsError("unauthenticated", "Нужна авторизация.");

  const userSnap = await db.doc(`users/${request.auth.uid}`).get();
  const user = userSnap.data();
  if (!user || user.role !== "child" || user.childId !== childId) {
    throw new HttpsError("permission-denied", "Награду может забрать только ребёнок-владелец профиля.");
  }

  const questRef = db.doc(`children/${childId}/quests/${questId}`);
  const rewardRef = db.doc(`children/${childId}/rewardLedger/quest_${questId}`);
  const childRef = db.doc(`children/${childId}`);
  const gameStateRef = db.doc(`children/${childId}/gameState/main`);
  const analyticsRef = db.doc(`children/${childId}/analyticsDaily/${todayId()}`);

  await db.runTransaction(async (tx) => {
    const [questSnap, rewardSnap] = await Promise.all([tx.get(questRef), tx.get(rewardRef)]);
    if (!questSnap.exists || !rewardSnap.exists) throw new HttpsError("not-found", "Награда не найдена.");
    const quest = questSnap.data()!;
    const reward = rewardSnap.data()!;
    if (quest.status !== "approved") throw new HttpsError("failed-precondition", "Квест ещё не одобрен.");
    if (reward.claimed) throw new HttpsError("already-exists", "Награда уже получена.");

    const xp = reward.xp ?? 0;
    const gold = reward.gold ?? 0;

    tx.update(rewardRef, { claimed: true, claimedAt: Timestamp.now() });
    tx.update(questRef, { status: "claimed", claimedAt: Timestamp.now() });
    tx.set(childRef, { xp: FieldValue.increment(xp), gold: FieldValue.increment(gold), updatedAt: Timestamp.now() }, { merge: true });
    tx.set(gameStateRef, { xp: FieldValue.increment(xp), gold: FieldValue.increment(gold), lastActiveAt: Timestamp.now() }, { merge: true });
    tx.set(analyticsRef, { xpEarned: FieldValue.increment(xp), goldEarned: FieldValue.increment(gold) }, { merge: true });
  });

  return { ok: true };
});
