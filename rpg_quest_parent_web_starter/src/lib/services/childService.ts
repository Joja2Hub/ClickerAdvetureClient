import { collection, doc, getDoc, getDocs, query, where } from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { ChildProfile } from "@/lib/types";
import { demoChild, isDemoChild, isDemoFamily } from "@/lib/demoData";

export async function getChildProfile(childId: string): Promise<ChildProfile | null> {
  if (isDemoChild(childId)) return demoChild;

  const snap = await getDoc(doc(db, "children", childId));
  if (!snap.exists()) return null;
  return { id: snap.id, ...snap.data() } as ChildProfile;
}

export async function listChildrenForFamily(familyId: string): Promise<ChildProfile[]> {
  if (isDemoFamily(familyId)) return [demoChild];

  const q = query(collection(db, "children"), where("familyId", "==", familyId));
  const snap = await getDocs(q);
  return snap.docs.map((docSnap) => ({ id: docSnap.id, ...docSnap.data() } as ChildProfile));
}
