"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthProvider";
import { getChildProfile } from "@/lib/services/childService";
import { createQuest } from "@/lib/services/questService";
import { getRecommendationForCategory } from "@/services/analyticsService";
import { QuestCategory, QuestDifficulty, QuestRecommendation } from "@/lib/types";

const categories: { value: QuestCategory; label: string }[] = [
  { value: "study", label: "Учеба" },
  { value: "home", label: "Дом" },
  { value: "reading", label: "Чтение" },
  { value: "health", label: "Здоровье" },
  { value: "sport", label: "Спорт" },
  { value: "hygiene", label: "Гигиена" },
  { value: "family_help", label: "Помощь семье" },
  { value: "custom", label: "Другое" },
];

export default function NewQuestPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const router = useRouter();
  const { user, profile } = useAuth();
  const [title, setTitle] = useState("Почитать 20 минут");
  const [description, setDescription] = useState("Прочитай книгу или учебный материал 20 минут и напиши, что было самым интересным.");
  const [category, setCategory] = useState<QuestCategory>("reading");
  const [difficulty, setDifficulty] = useState<QuestDifficulty>("normal");
  const [rewardXp, setRewardXp] = useState(80);
  const [rewardGold, setRewardGold] = useState(40);
  const [recommendation, setRecommendation] = useState<QuestRecommendation | null>(null);
  const [recommendationLoading, setRecommendationLoading] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let active = true;
    setRecommendationLoading(true);
    getRecommendationForCategory(childId, category)
      .then((value) => {
        if (active) setRecommendation(value);
      })
      .catch((err) => {
        console.warn("Recommendation is unavailable:", err);
        if (active) setRecommendation(null);
      })
      .finally(() => {
        if (active) setRecommendationLoading(false);
      });
    return () => { active = false; };
  }, [childId, category]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!user) return;
    setError("");
    setLoading(true);
    try {
      if (!profile || profile.role !== "parent") {
        throw new Error("Создавать квесты может только родительский аккаунт.");
      }
      const child = await getChildProfile(childId);
      if (!child) throw new Error("Ребенок не найден.");
      if (child.familyId !== profile.familyId) throw new Error("Ребенок принадлежит другой семье.");

      await createQuest({
        childId,
        title,
        description,
        category,
        difficulty,
        rewardXp,
        rewardGold,
        createdByParentId: user.uid,
        recommendationScore: recommendation?.score ?? 0,
        recommended: (recommendation?.score ?? 0) >= 70,
      });
      router.push(`/children/${childId}/quests`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось создать квест.");
    } finally {
      setLoading(false);
    }
  }

  function applyRecommendation() {
    if (!recommendation) return;
    setDifficulty(recommendation.suggestedDifficulty);
    setRewardXp(recommendation.suggestedRewardXp);
    setRewardGold(recommendation.suggestedRewardGold);
  }

  return (
    <section className="questCreateLayout">
      <form className="card grid" onSubmit={submit}>
        <div>
          <h1>Создать real-life квест</h1>
          <p>Квест появится у ребенка на доске приключений в Unity.</p>
        </div>
        <label>Название<input value={title} onChange={(e) => setTitle(e.target.value)} required /></label>
        <label>Описание<textarea value={description} onChange={(e) => setDescription(e.target.value)} required /></label>
        <div className="grid2">
          <label>Категория<select value={category} onChange={(e) => setCategory(e.target.value as QuestCategory)}>{categories.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
          <label>Сложность<select value={difficulty} onChange={(e) => setDifficulty(e.target.value as QuestDifficulty)}><option value="easy">Легкая</option><option value="normal">Обычная</option><option value="hard">Сложная</option></select></label>
          <label>XP<input type="number" min={0} value={rewardXp} onChange={(e) => setRewardXp(Number(e.target.value))} /></label>
          <label>Золото<input type="number" min={0} value={rewardGold} onChange={(e) => setRewardGold(Number(e.target.value))} /></label>
        </div>
        {error && <p className="error">{error}</p>}
        <button disabled={loading}>{loading ? "Создание..." : "Создать квест"}</button>
      </form>

      <aside className="card recommendationAside">
        <h2>Рекомендация системы</h2>
        {recommendationLoading ? <p>Анализ категории...</p> : recommendation ? (
          <>
            <div className={`recommendationScore ${recommendation.label}`}>
              <strong>{recommendation.score}/100</strong>
              <span>{recommendation.label === "recommended" ? "Рекомендуется" : recommendation.label === "neutral" ? "Нейтрально" : "Не рекомендуется"}</span>
            </div>
            {recommendation.reasons.length > 0 && <><h3>Причины</h3><ul className="compactList">{recommendation.reasons.map((reason) => <li key={reason}>{reason}</li>)}</ul></>}
            {recommendation.risks.length > 0 && <><h3>Риски</h3><ul className="compactList">{recommendation.risks.map((risk) => <li key={risk}>{risk}</li>)}</ul></>}
            {recommendation.suggestedActions.length > 0 && <><h3>Совет</h3><ul className="compactList">{recommendation.suggestedActions.map((action) => <li key={action}>{action}</li>)}</ul></>}
            <p>Предлагается сложность <b>{recommendation.suggestedDifficulty}</b>, {recommendation.suggestedRewardXp} XP и {recommendation.suggestedRewardGold} золота.</p>
            <button type="button" className="secondary" onClick={applyRecommendation}>Применить совет</button>
          </>
        ) : <p>Рекомендация сейчас недоступна. Квест можно создать без нее.</p>}
      </aside>
    </section>
  );
}
