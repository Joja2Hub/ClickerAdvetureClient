import { httpsCallable } from "firebase/functions";
import { functions } from "@/lib/firebase/client";

interface CreateChildLoginInput {
  childId: string;
  email: string;
  password: string;
  displayName?: string;
}

export async function createChildLogin(input: CreateChildLoginInput): Promise<string> {
  const callable = httpsCallable<CreateChildLoginInput, { ok: boolean; uid: string }>(
    functions,
    "createChildLogin"
  );
  const result = await callable(input);
  return result.data.uid;
}
