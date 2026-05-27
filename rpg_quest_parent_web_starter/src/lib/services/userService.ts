import { doc, getDoc, serverTimestamp, setDoc } from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { UserProfile } from "@/lib/types";

export async function getUserProfile(uid: string): Promise<UserProfile | null> {
  const snap = await getDoc(doc(db, "users", uid));
  if (!snap.exists()) return null;
  return snap.data() as UserProfile;
}

export async function createParentProfile(params: {
  uid: string;
  email?: string | null;
  displayName: string;
  familyId: string;
}): Promise<void> {
  await setDoc(doc(db, "users", params.uid), {
    uid: params.uid,
    role: "parent",
    familyId: params.familyId,
    displayName: params.displayName,
    email: params.email ?? "",
    createdAt: serverTimestamp(),
  });
}
