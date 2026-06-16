"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthProvider";
import { getChildProfile } from "@/lib/services/childService";
import { createQuest } from "@/lib/services/questService";
import { getRecommendationForCategory } from "@/services/analyticsService";
import { QuestCategory, QuestDifficulty, QuestRecommendation } from "@/lib/types";
import { questTemplates, rewardsByDifficulty } from "@/lib/questTemplates";

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
  const [customReward, setCustomReward] = useState(false);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | null>("read-20");
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
    setSelectedTemplateId(null);
    setDifficulty(recommendation.suggestedDifficulty);
    setRewardXp(recommendation.suggestedRewardXp);
    setRewardGold(recommendation.suggestedRewardGold);
    setCustomReward(true);
  }

  function changeDifficulty(value: QuestDifficulty) {
    setSelectedTemplateId(null);
    setDifficulty(value);
    if (!customReward) {
      setRewardXp(rewardsByDifficulty[value].xp);
      setRewardGold(rewardsByDifficulty[value].gold);
    }
  }

  function applyTemplate(templateId: string) {
    const template = questTemplates.find((item) => item.id === templateId);
    if (!template) return;
    const reward = rewardsByDifficulty[template.difficulty];
    setSelectedTemplateId(template.id);
    setTitle(template.title);
    setDescription(template.description);
    setCategory(template.category);
    setDifficulty(template.difficulty);
    setRewardXp(reward.xp);
    setRewardGold(reward.gold);
    setCustomReward(false);
  }

  function useAutomaticReward() {
    const reward = rewardsByDifficulty[difficulty];
    setRewardXp(reward.xp);
    setRewardGold(reward.gold);
    setCustomReward(false);
  }

  return (
    <section className="questCreateLayout">
      <form className="card grid" onSubmit={submit}>
        <div>
          <h1>Создать реальный квест</h1>
          <p>Квест появится у ребенка на доске приключений в Unity.</p>
        </div>
        <details className="templateSection">
          <summary>
            <div>
              <span>Готовые основы</span>
              <small>
                {selectedTemplateId
                  ? `Выбрано: ${questTemplates.find((template) => template.id === selectedTemplateId)?.title}`
                  : "Можно выбрать основу для быстрого создания"}
              </small>
            </div>
          </summary>
          <div className="templateSectionBody">
            <p>Выберите основу и при необходимости измените текст.</p>
            <div className="templateGrid">
              {questTemplates.map((template) => (
                <button
                  type="button"
                  key={template.id}
                  className={`templateOption ${selectedTemplateId === template.id ? "selected" : ""}`}
                  onClick={() => applyTemplate(template.id)}
                >
                  <strong>{template.title}</strong>
                  <span>{categories.find((item) => item.value === template.category)?.label}</span>
                </button>
              ))}
            </div>
          </div>
        </details>
        <label>Название<input value={title} onChange={(e) => { setTitle(e.target.value); setSelectedTemplateId(null); }} required /></label>
        <label>Описание<textarea value={description} onChange={(e) => { setDescription(e.target.value); setSelectedTemplateId(null); }} required /></label>
        <div className="grid2">
          <label>Категория<select value={category} onChange={(e) => { setCategory(e.target.value as QuestCategory); setSelectedTemplateId(null); }}>{categories.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
          <label>Сложность<select value={difficulty} onChange={(e) => changeDifficulty(e.target.value as QuestDifficulty)}><option value="easy">Легкая</option><option value="normal">Обычная</option><option value="hard">Сложная</option></select></label>
        </div>
        <div className="rewardPreview">
          <div>
            <span>Награда за задание</span>
            <strong>{rewardXp} XP и {rewardGold} золота</strong>
          </div>
          <small>{customReward ? "Настроена вручную" : "Рассчитана по сложности"}</small>
        </div>
        <details className="advancedSettings">
          <summary>Расширенные настройки</summary>
          <div className="advancedSettingsBody">
            <p>Здесь можно вручную изменить награду. По умолчанию она определяется сложностью задания.</p>
            <div className="grid2">
              <label>XP<input type="number" min={0} value={rewardXp} onChange={(e) => { setRewardXp(Number(e.target.value)); setCustomReward(true); }} /></label>
              <label>Золото<input type="number" min={0} value={rewardGold} onChange={(e) => { setRewardGold(Number(e.target.value)); setCustomReward(true); }} /></label>
            </div>
            {customReward && <button type="button" className="secondary" onClick={useAutomaticReward}>Вернуть автоматическую награду</button>}
          </div>
        </details>
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
