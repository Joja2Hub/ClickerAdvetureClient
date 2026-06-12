"use client";

import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import {
  getCategoryAnalytics,
  getRecommendations,
  listRecentDailyAnalytics,
  rebuildAnalyticsForChild,
} from "@/services/analyticsService";
import { CategoryAnalytics, DailyAnalytics, QuestRecommendation } from "@/lib/types";
import { formatCategory } from "@/lib/utils/format";

function percent(value: number): string {
  return `${Math.round((value || 0) * 100)}%`;
}

function hours(value: number | null): string {
  return value === null ? "нет данных" : `${value.toFixed(1)} ч`;
}

export default function AnalyticsPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [days, setDays] = useState<DailyAnalytics[]>([]);
  const [categories, setCategories] = useState<CategoryAnalytics[]>([]);
  const [recommendations, setRecommendations] = useState<QuestRecommendation[]>([]);
  const [loading, setLoading] = useState(true);
  const [rebuilding, setRebuilding] = useState(false);
  const [error, setError] = useState("");

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [nextDays, nextCategories, nextRecommendations] = await Promise.all([
        listRecentDailyAnalytics(childId, 14),
        getCategoryAnalytics(childId),
        getRecommendations(childId),
      ]);
      setDays(nextDays);
      setCategories(nextCategories);
      setRecommendations(nextRecommendations);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось загрузить аналитику.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [childId]);

  async function rebuild() {
    setRebuilding(true);
    setError("");
    try {
      await rebuildAnalyticsForChild(childId);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось пересчитать аналитику.");
    } finally {
      setRebuilding(false);
    }
  }

  const totals = useMemo(() => categories.reduce(
    (acc, row) => ({
      assigned: acc.assigned + row.assignedTotal,
      claimed: acc.claimed + row.claimedTotal,
      submitted: acc.submitted + row.submittedTotal,
      rejected: acc.rejected + row.rejectedTotal,
      submitHours: row.avgSubmitHours === null
        ? acc.submitHours
        : [...acc.submitHours, row.avgSubmitHours],
    }),
    { assigned: 0, claimed: 0, submitted: 0, rejected: 0, submitHours: [] as number[] }
  ), [categories]);
  const completionRate = totals.assigned > 0 ? totals.claimed / totals.assigned : 0;
  const avgSubmitHours = totals.submitHours.length > 0
    ? totals.submitHours.reduce((sum, value) => sum + value, 0) / totals.submitHours.length
    : null;
  const frequentCategories = [...categories].sort((a, b) => b.assignedTotal - a.assignedTotal).slice(0, 3);
  const rejectedCategories = [...categories].filter((row) => row.rejectionRate > 0).sort((a, b) => b.rejectionRate - a.rejectionRate).slice(0, 3);
  const lowReminderCategories = [...categories].filter((row) => row.assignedTotal > 0).sort((a, b) => a.reminderNeedRate - b.reminderNeedRate).slice(0, 3);

  return (
    <section className="grid">
      <div className="toolbar">
        <div>
          <h1>Аналитика ребенка</h1>
          <p>События квестов агрегируются по дням и категориям. Пересчет можно запускать повторно.</p>
        </div>
        <button onClick={rebuild} disabled={rebuilding}>
          {rebuilding ? "Пересчет..." : "Пересчитать аналитику"}
        </button>
      </div>

      {error && <p className="error">{error}</p>}
      {loading ? <div className="card">Загрузка аналитики...</div> : (
        <>
          <div className="grid3">
            <div className="statCard"><span className="metricLabel">Назначено</span><strong>{totals.assigned}</strong></div>
            <div className="statCard"><span className="metricLabel">Выполнено</span><strong>{totals.claimed}</strong></div>
            <div className="statCard"><span className="metricLabel">Успешность</span><strong>{percent(completionRate)}</strong></div>
            <div className="statCard"><span className="metricLabel">Среднее время</span><strong>{hours(avgSubmitHours)}</strong></div>
            <div className="statCard"><span className="metricLabel">Отправлено</span><strong>{totals.submitted}</strong></div>
            <div className="statCard"><span className="metricLabel">Отклонено</span><strong>{totals.rejected}</strong></div>
          </div>

          <div className="grid3">
            <SummaryList title="Частые категории" rows={frequentCategories.map((row) => `${formatCategory(row.category)}: ${row.assignedTotal}`)} />
            <SummaryList title="Частые отклонения" rows={rejectedCategories.map((row) => `${formatCategory(row.category)}: ${percent(row.rejectionRate)}`)} />
            <SummaryList title="Меньше напоминаний" rows={lowReminderCategories.map((row) => `${formatCategory(row.category)}: ${percent(row.reminderNeedRate)}`)} />
          </div>

          <div className="card">
            <h2>Показатели по категориям</h2>
            {categories.length === 0 ? <p>Данных пока нет. Создайте и обработайте несколько квестов, затем запустите пересчет.</p> : (
              <div className="tableWrap">
                <table className="analyticsTable">
                  <thead><tr><th>Категория</th><th>Назначено</th><th>Выполнено</th><th>Успешность</th><th>Отклонения</th><th>Среднее время</th><th>Оценка</th></tr></thead>
                  <tbody>
                    {categories.map((row) => (
                      <tr key={row.id}>
                        <td>{formatCategory(row.category)}</td>
                        <td>{row.assignedTotal}</td>
                        <td>{row.claimedTotal}</td>
                        <td>{percent(row.completionRate)}</td>
                        <td>{percent(row.rejectionRate)}</td>
                        <td>{hours(row.avgSubmitHours)}</td>
                        <td><span className={`recommendationBadge ${row.recommendationLabel}`}>{row.recommendationScore}/100</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card">
            <h2>Рекомендации</h2>
            <div className="recommendationGrid">
              {recommendations.length === 0 ? <p>Рекомендации появятся после пересчета событий.</p> : recommendations.map((item) => (
                <RecommendationCard key={item.id} item={item} />
              ))}
            </div>
          </div>

          <div className="card">
            <h2>Последние дни</h2>
            {days.length === 0 ? <p>Дневных агрегатов пока нет.</p> : (
              <div className="dailyStrip">
                {days.map((day) => (
                  <div className="statCard" key={day.id}>
                    <h3>{day.date}</h3>
                    <p>Назначено: {day.assignedCount}</p>
                    <p>Выполнено: {day.claimedCount}</p>
                    <p>Успешность: {percent(day.completionRate)}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </section>
  );
}

function SummaryList({ title, rows }: { title: string; rows: string[] }) {
  return (
    <div className="statCard">
      <h3>{title}</h3>
      {rows.length === 0 ? <p>Недостаточно данных</p> : <ul className="compactList">{rows.map((row) => <li key={row}>{row}</li>)}</ul>}
    </div>
  );
}

function RecommendationCard({ item }: { item: QuestRecommendation }) {
  const title = item.label === "recommended" ? "Рекомендуется" : item.label === "neutral" ? "Нейтрально" : "Не рекомендуется";
  return (
    <article className={`recommendationCard ${item.label}`}>
      <div className="questHeader">
        <div><h3>{formatCategory(item.targetCategory)}</h3><p>{title}</p></div>
        <strong>{item.score}/100</strong>
      </div>
      {item.reasons.length > 0 && <><b>Причины</b><ul className="compactList">{item.reasons.map((reason) => <li key={reason}>{reason}</li>)}</ul></>}
      {item.risks.length > 0 && <><b>Риски</b><ul className="compactList">{item.risks.map((risk) => <li key={risk}>{risk}</li>)}</ul></>}
      {item.suggestedActions.length > 0 && <><b>Советы</b><ul className="compactList">{item.suggestedActions.map((action) => <li key={action}>{action}</li>)}</ul></>}
      <p>Сложность: <b>{item.suggestedDifficulty}</b>. Награда: {item.suggestedRewardXp} XP, {item.suggestedRewardGold} золота.</p>
    </article>
  );
}
