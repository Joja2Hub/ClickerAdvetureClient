import { FirebaseError, deleteApp, getApp, getApps, initializeApp } from "firebase/app";
import {
  createUserWithEmailAndPassword,
  getAuth,
  signInWithEmailAndPassword,
  signOut,
  updateProfile,
} from "firebase/auth";
import { doc, getDoc, setDoc, getFirestore, serverTimestamp } from "firebase/firestore";
import { db, firebaseConfig } from "@/lib/firebase/client";

interface CreateChildLoginInput {
  childId: string;
  email: string;
  password: string;
  displayName?: string;
}

export async function createChildLogin(input: CreateChildLoginInput): Promise<string> {
  const childSnap = await getDoc(doc(db, "children", input.childId));
  if (!childSnap.exists()) {
    throw new Error("Ребенок не найден. Вернитесь на dashboard и выберите ребенка из списка.");
  }

  const child = childSnap.data();
  const secondaryName = "child-login";
  const secondaryApp = getApps().some((app) => app.name === secondaryName)
    ? getApp(secondaryName)
    : initializeApp(firebaseConfig, secondaryName);
  const childAuth = getAuth(secondaryApp);
  const childDb = getFirestore(secondaryApp);

  try {
    let credential;
    try {
      credential = await createUserWithEmailAndPassword(childAuth, input.email, input.password);
    } catch (error) {
      if (!(error instanceof FirebaseError) || error.code !== "auth/email-already-in-use") {
        throw error;
      }

      credential = await signInWithEmailAndPassword(childAuth, input.email, input.password);
    }

    if (input.displayName) {
      await updateProfile(credential.user, { displayName: input.displayName });
    }

    await setDoc(
      doc(childDb, "users", credential.user.uid),
      {
        uid: credential.user.uid,
        role: "child",
        familyId: child.familyId || "",
        childId: input.childId,
        displayName: input.displayName || credential.user.displayName || "Юный герой",
        email: input.email,
        updatedAt: serverTimestamp(),
      },
      { merge: true }
    );

    await setDoc(
      doc(db, "children", input.childId),
      {
        unityAuthUid: credential.user.uid,
        unityLoginEmail: input.email,
        unityLoginPasswordForTesting: input.password,
        unityLoginUpdatedAt: serverTimestamp(),
      },
      { merge: true }
    );

    return credential.user.uid;
  } finally {
    await signOut(childAuth).catch(() => undefined);
    await deleteApp(secondaryApp).catch(() => undefined);
  }
}
