import {
  addDoc,
  collection,
  doc,
  getDocs,
  onSnapshot,
  orderBy,
  query,
  serverTimestamp,
  updateDoc,
  where,
} from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { Quest, QuestCategory, QuestStatus } from "@/lib/types";

export interface CreateQuestInput {
  childId: string;
  title: string;
  description: string;
  category: QuestCategory;
  rewardXp: number;
  rewardGold: number;
  createdByParentId: string;
  dueDate?: Date | null;
}

export async function createQuest(input: CreateQuestInput): Promise<string> {
  const ref = await addDoc(collection(db, "children", input.childId, "quests"), {
    title: input.title,
    description: input.description,
    category: input.category,
    status: "assigned",
    rewardXp: input.rewardXp,
    rewardGold: input.rewardGold,
    rewardItems: [],
    createdByParentId: input.createdByParentId,
    createdAt: serverTimestamp(),
    dueDate: input.dueDate ?? null,
  });
  return ref.id;
}

export function listenChildQuests(
  childId: string,
  callback: (quests: Quest[]) => void,
  statuses?: QuestStatus[]
): () => void {
  const base = collection(db, "children", childId, "quests");
  const constraints = statuses?.length
    ? [where("status", "in", statuses), orderBy("createdAt", "desc")]
    : [orderBy("createdAt", "desc")];

  return onSnapshot(query(base, ...constraints), (snap) => {
    callback(snap.docs.map((d) => ({ id: d.id, ...d.data() } as Quest)));
  });
}

export async function listChildQuests(childId: string): Promise<Quest[]> {
  const snap = await getDocs(query(collection(db, "children", childId, "quests"), orderBy("createdAt", "desc")));
  return snap.docs.map((d) => ({ id: d.id, ...d.data() } as Quest));
}

export async function approveQuestDirect(childId: string, questId: string): Promise<void> {
  await updateDoc(doc(db, "children", childId, "quests", questId), {
    status: "approved",
    approvedAt: serverTimestamp(),
    parentComment: "",
  });
}

export async function rejectQuestDirect(childId: string, questId: string, parentComment: string): Promise<void> {
  await updateDoc(doc(db, "children", childId, "quests", questId), {
    status: "rejected",
    rejectedAt: serverTimestamp(),
    parentComment,
  });
}

export async function cancelQuestDirect(childId: string, questId: string): Promise<void> {
  await updateDoc(doc(db, "children", childId, "quests", questId), {
    status: "cancelled",
  });
}
