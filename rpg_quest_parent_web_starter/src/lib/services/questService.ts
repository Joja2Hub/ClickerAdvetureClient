import {
  addDoc,
  collection,
  doc,
  getDoc,
  getDocs,
  onSnapshot,
  orderBy,
  query,
  serverTimestamp,
  updateDoc,
  where,
} from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { trackQuestEvent } from "@/services/analyticsService";
import { Quest, QuestCategory, QuestDifficulty, QuestStatus } from "@/lib/types";
import { demoQuests, isDemoChild } from "@/lib/demoData";

export interface CreateQuestInput {
  childId: string;
  title: string;
  description: string;
  category: QuestCategory;
  rewardXp: number;
  rewardGold: number;
  createdByParentId: string;
  difficulty?: QuestDifficulty;
  recommendationScore?: number;
  recommended?: boolean;
  dueDate?: Date | null;
}

export async function createQuest(input: CreateQuestInput): Promise<string> {
  if (isDemoChild(input.childId)) {
    return `demo-created-${Date.now()}`;
  }

  const ref = await addDoc(collection(db, "children", input.childId, "quests"), {
    title: input.title,
    description: input.description,
    category: input.category,
    status: "assigned",
    rewardXp: input.rewardXp,
    rewardGold: input.rewardGold,
    rewardItems: [],
    createdByParentId: input.createdByParentId,
    difficulty: input.difficulty ?? "normal",
    recommendationScore: input.recommendationScore ?? 0,
    recommended: input.recommended ?? false,
    createdAt: serverTimestamp(),
    dueDate: input.dueDate ?? null,
  });
  await trackParentEventSafely(input.childId, ref.id, "quest_created", {
    category: input.category,
    difficulty: input.difficulty ?? "normal",
    rewardXp: input.rewardXp,
    rewardGold: input.rewardGold,
    statusAfter: "assigned",
  });
  return ref.id;
}

export function listenChildQuests(
  childId: string,
  callback: (quests: Quest[]) => void,
  statuses?: QuestStatus[]
): () => void {
  if (isDemoChild(childId)) {
    const nextQuests = statuses?.length
      ? demoQuests.filter((quest) => statuses.includes(quest.status))
      : demoQuests;
    window.setTimeout(() => callback(nextQuests), 0);
    return () => undefined;
  }

  const base = collection(db, "children", childId, "quests");
  const constraints = statuses?.length
    ? [where("status", "in", statuses), orderBy("createdAt", "desc")]
    : [orderBy("createdAt", "desc")];

  return onSnapshot(query(base, ...constraints), (snap) => {
    callback(snap.docs.map((d) => ({ id: d.id, ...d.data() } as Quest)));
  });
}

export async function listChildQuests(childId: string): Promise<Quest[]> {
  if (isDemoChild(childId)) return demoQuests;

  const snap = await getDocs(query(collection(db, "children", childId, "quests"), orderBy("createdAt", "desc")));
  return snap.docs.map((d) => ({ id: d.id, ...d.data() } as Quest));
}

export async function approveQuestDirect(childId: string, questId: string): Promise<void> {
  if (isDemoChild(childId)) return;

  const quest = await getQuest(childId, questId);
  await updateDoc(doc(db, "children", childId, "quests", questId), {
    status: "approved",
    approvedAt: serverTimestamp(),
    parentComment: "",
  });
  await trackParentEventSafely(childId, questId, "quest_approved", {
    ...eventFields(quest),
    statusBefore: quest.status,
    statusAfter: "approved",
  });
}

export async function rejectQuestDirect(childId: string, questId: string, parentComment: string): Promise<void> {
  if (isDemoChild(childId)) return;

  const quest = await getQuest(childId, questId);
  await updateDoc(doc(db, "children", childId, "quests", questId), {
    status: "rejected",
    rejectedAt: serverTimestamp(),
    parentComment,
  });
  await trackParentEventSafely(childId, questId, "quest_rejected", {
    ...eventFields(quest),
    statusBefore: quest.status,
    statusAfter: "rejected",
  });
}

export async function cancelQuestDirect(childId: string, questId: string): Promise<void> {
  if (isDemoChild(childId)) return;

  const quest = await getQuest(childId, questId);
  await updateDoc(doc(db, "children", childId, "quests", questId), {
    status: "cancelled",
  });
  await trackParentEventSafely(childId, questId, "quest_cancelled", {
    ...eventFields(quest),
    statusBefore: quest.status,
    statusAfter: "cancelled",
  });
}

async function getQuest(childId: string, questId: string): Promise<Quest> {
  if (isDemoChild(childId)) {
    const quest = demoQuests.find((item) => item.id === questId);
    if (!quest) throw new Error("Квест не найден.");
    return quest;
  }

  const snap = await getDoc(doc(db, "children", childId, "quests", questId));
  if (!snap.exists()) throw new Error("Квест не найден.");
  return { id: snap.id, ...snap.data() } as Quest;
}

function eventFields(quest: Quest) {
  return {
    category: quest.category,
    difficulty: quest.difficulty ?? "normal" as QuestDifficulty,
    rewardXp: quest.rewardXp,
    rewardGold: quest.rewardGold,
    assignedAt: quest.createdAt,
    submittedAt: quest.submittedAt,
    approvedAt: quest.approvedAt,
    claimedAt: quest.claimedAt,
  };
}

async function trackParentEventSafely(
  childId: string,
  questId: string,
  eventType: "quest_created" | "quest_approved" | "quest_rejected" | "quest_cancelled",
  fields: {
    category: QuestCategory;
    difficulty: QuestDifficulty;
    rewardXp: number;
    rewardGold: number;
    statusBefore?: string;
    statusAfter?: string;
    assignedAt?: unknown;
    submittedAt?: unknown;
    approvedAt?: unknown;
    claimedAt?: unknown;
  }
) {
  try {
    if (isDemoChild(childId)) {
      await trackQuestEvent({
        familyId: "",
        childId,
        questId,
        eventType,
        actorRole: "parent",
        sourceClient: "web",
        ...fields,
      });
      return;
    }

    const childSnap = await getDoc(doc(db, "children", childId));
    await trackQuestEvent({
      familyId: childSnap.data()?.familyId ?? "",
      childId,
      questId,
      eventType,
      actorRole: "parent",
      sourceClient: "web",
      ...fields,
    });
  } catch (error) {
    console.warn("Quest analytics event was not recorded:", error);
  }
}
