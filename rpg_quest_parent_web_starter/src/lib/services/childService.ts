import { collection, getDocs, query, where } from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { ChildProfile } from "@/lib/types";

export async function listChildrenForFamily(familyId: string): Promise<ChildProfile[]> {
  const q = query(collection(db, "children"), where("familyId", "==", familyId));
  const snap = await getDocs(q);
  return snap.docs.map((docSnap) => ({ id: docSnap.id, ...docSnap.data() } as ChildProfile));
}
