"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { AuthGate } from "@/components/AuthGate";
import { TopNav } from "@/components/TopNav";
import { useAuth } from "@/context/AuthProvider";
import { ChildProfile } from "@/lib/types";
import { listChildrenForFamily } from "@/lib/services/childService";

export default function DashboardPage() {
  const { profile } = useAuth();
  const [children, setChildren] = useState<ChildProfile[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!profile?.familyId) return;
    listChildrenForFamily(profile.familyId).then(setChildren).finally(() => setLoading(false));
  }, [profile?.familyId]);

  return (
    <AuthGate>
      <main className="page">
        <TopNav />
        <section className="toolbar">
          <div>
            <h1>Дети</h1>
            <p>Выберите ребёнка, чтобы управлять его квестами и смотреть прогресс.</p>
          </div>
        </section>
        {loading ? <div className="card">Загрузка...</div> : (
          <div className="grid3">
            {children.map((child) => (
              <Link className="childCard" key={child.id} href={`/children/${child.id}/quests`}>
                <h2>{child.displayName}</h2>
                <p>Уровень {child.level}</p>
                <div className="rewardRow">
                  <span>{child.xp} XP</span>
                  <span>{child.gold} золота</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </main>
    </AuthGate>
  );
}
