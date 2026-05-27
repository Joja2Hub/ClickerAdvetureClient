"use client";

import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { DailyAnalytics } from "@/lib/types";
import { listRecentDailyAnalytics } from "@/lib/services/analyticsService";

export default function AnalyticsPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [rows, setRows] = useState<DailyAnalytics[]>([]);

  useEffect(() => {
    listRecentDailyAnalytics(childId).then(setRows);
  }, [childId]);

  const totals = useMemo(() => rows.reduce(
    (acc, row) => ({
      assigned: acc.assigned + (row.questsAssigned || 0),
      submitted: acc.submitted + (row.questsSubmitted || 0),
      approved: acc.approved + (row.questsApproved || 0),
      rejected: acc.rejected + (row.questsRejected || 0),
      battles: acc.battles + (row.battlesPlayed || 0),
      xp: acc.xp + (row.xpEarned || 0),
      gold: acc.gold + (row.goldEarned || 0),
    }),
    { assigned: 0, submitted: 0, approved: 0, rejected: 0, battles: 0, xp: 0, gold: 0 }
  ), [rows]);

  return (
    <section className="grid">
      <div>
        <h1>Аналитика</h1>
        <p>Здесь будут дневные агрегаты. Unity и Cloud Functions будут записывать их после боёв и real-life квестов.</p>
      </div>
      <div className="grid3">
        <div className="statCard"><h2>{totals.approved}</h2><p>Одобрено квестов</p></div>
        <div className="statCard"><h2>{totals.battles}</h2><p>Боёв сыграно</p></div>
        <div className="statCard"><h2>{totals.xp}</h2><p>XP заработано</p></div>
      </div>
      <div className="card">
        <h2>Последние дни</h2>
        {rows.length === 0 ? <p>Данных пока нет.</p> : (
          <div className="grid">
            {rows.map((row) => (
              <div className="statCard" key={row.id}>
                <h3>{row.id}</h3>
                <p>Назначено: {row.questsAssigned || 0}, отправлено: {row.questsSubmitted || 0}, одобрено: {row.questsApproved || 0}, боёв: {row.battlesPlayed || 0}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
