import { addDoc, collection, doc, serverTimestamp, setDoc } from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { ChildProfile } from "@/lib/types";

export async function createFamily(params: {
  ownerParentId: string;
  familyName: string;
}): Promise<string> {
  const ref = await addDoc(collection(db, "families"), {
    name: params.familyName,
    ownerParentId: params.ownerParentId,
    createdAt: serverTimestamp(),
  });
  return ref.id;
}

export async function createChild(params: {
  familyId: string;
  displayName: string;
}): Promise<string> {
  const ref = await addDoc(collection(db, "children"), {
    familyId: params.familyId,
    displayName: params.displayName,
    level: 1,
    xp: 0,
    gold: 0,
    createdAt: serverTimestamp(),
    updatedAt: serverTimestamp(),
  } satisfies Omit<ChildProfile, "id">);

  await setDoc(doc(db, "children", ref.id, "gameState", "main"), {
    level: 1,
    xp: 0,
    gold: 0,
    maxHp: 100,
    damage: 10,
    critChance: 0.05,
    rageGain: 1,
    unlockedLocations: ["forest_entrance", "start_village"],
    completedDungeons: [],
    currentMapNodeId: "start_village",
    dailyStreak: 0,
    lastActiveAt: serverTimestamp(),
  });

  return ref.id;
}
