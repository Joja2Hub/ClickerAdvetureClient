import { FieldValue, Timestamp, type Firestore } from "firebase-admin/firestore";
import { HttpsError } from "firebase-functions/v2/https";

function validId(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0
    && !value.includes("/") && value !== "." && value !== "..";
}

export async function claimReward(db: Firestore, uid: string | undefined, input: unknown) {
  if (!uid) throw new HttpsError("unauthenticated", "Нужна авторизация.");
  const { childId, questId } = (input ?? {}) as { childId?: unknown; questId?: unknown };
  if (!validId(childId) || !validId(questId)) {
    throw new HttpsError("invalid-argument", "Нужны корректные childId и questId.");
  }

  const userRef = db.doc(`users/${uid}`);
  const childRef = db.doc(`children/${childId}`);
  const questRef = childRef.collection("quests").doc(questId);
  const rewardRef = childRef.collection("rewardLedger").doc(`quest_${questId}`);
  const gameStateRef = childRef.collection("gameState").doc("main");
  const now = Timestamp.now();
  const analyticsRef = childRef.collection("analyticsDaily").doc(now.toDate().toISOString().slice(0, 10));

  await db.runTransaction(async tx => {
    const [userSnap, childSnap, questSnap, rewardSnap] = await Promise.all([
      tx.get(userRef), tx.get(childRef), tx.get(questRef), tx.get(rewardRef),
    ]);
    const user = userSnap.data();
    const child = childSnap.data();
    if (!user || user.role !== "child" || user.childId !== childId || !child
      || typeof user.familyId !== "string" || !user.familyId || user.familyId !== child.familyId) {
      throw new HttpsError("permission-denied", "Награду может забрать только ребёнок-владелец профиля.");
    }

    const quest = questSnap.data();
    if (!quest) throw new HttpsError("not-found", "Квест не найден.");
    const reward = rewardSnap.data();
    if (quest.status === "claimed" || quest.claimedAt != null || reward?.claimed === true) {
      throw new HttpsError("already-exists", "Награда уже получена.");
    }
    if (quest.status !== "approved") throw new HttpsError("failed-precondition", "Квест ещё не одобрен.");

    let xp: number;
    let gold: number;
    if (reward) {
      xp = reward.xp ?? 0;
      gold = reward.gold ?? 0;
    } else {
      // The old parent dashboard approved ordinary quests directly and left no ledger entry.
      if (quest.geo != null || !(quest.approvedAt instanceof Timestamp)) {
        throw new HttpsError("failed-precondition", "Для задания отсутствует подтверждённая награда.");
      }
      xp = quest.rewardXp ?? 0;
      gold = quest.rewardGold ?? 0;
      if (!Number.isSafeInteger(xp) || xp < 0 || !Number.isSafeInteger(gold) || gold < 0) {
        throw new HttpsError("failed-precondition", "В задании указана некорректная награда.");
      }
      tx.create(rewardRef, {
        sourceType: "realQuest", sourceId: questId, xp, gold, items: quest.rewardItems ?? [],
        claimed: true, createdAt: now, claimedAt: now,
      });
    }

    if (reward) tx.update(rewardRef, { claimed: true, claimedAt: now });
    tx.update(questRef, { status: "claimed", claimedAt: now });
    tx.set(childRef, { xp: FieldValue.increment(xp), gold: FieldValue.increment(gold), updatedAt: now }, { merge: true });
    tx.set(gameStateRef, { xp: FieldValue.increment(xp), gold: FieldValue.increment(gold), lastActiveAt: now }, { merge: true });
    tx.set(analyticsRef, { xpEarned: FieldValue.increment(xp), goldEarned: FieldValue.increment(gold) }, { merge: true });
  });

  return { ok: true };
}
