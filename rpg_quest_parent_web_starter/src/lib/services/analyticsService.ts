import { collection, getDocs, limit, orderBy, query } from "firebase/firestore";
import { db } from "@/lib/firebase/client";
import { DailyAnalytics } from "@/lib/types";

export async function listRecentDailyAnalytics(childId: string, days = 14): Promise<DailyAnalytics[]> {
  const snap = await getDocs(
    query(collection(db, "children", childId, "analyticsDaily"), orderBy("__name__", "desc"), limit(days))
  );
  return snap.docs.map((d) => ({ id: d.id, ...d.data() } as DailyAnalytics)).reverse();
}
