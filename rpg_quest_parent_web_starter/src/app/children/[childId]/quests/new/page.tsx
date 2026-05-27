"use client";

import { FormEvent, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthProvider";
import { getChildProfile } from "@/lib/services/childService";
import { createQuest } from "@/lib/services/questService";
import { QuestCategory } from "@/lib/types";

const categories: { value: QuestCategory; label: string }[] = [
  { value: "study", label: "Учёба" },
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
  const [description, setDescription] = useState("Прочитай книгу или учебный материал 20 минут и отправь комментарий о том, что прочитал.");
  const [category, setCategory] = useState<QuestCategory>("reading");
  const [rewardXp, setRewardXp] = useState(80);
  const [rewardGold, setRewardGold] = useState(40);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!user) return;
    setError("");
    setLoading(true);
    try {
      if (!profile || profile.role !== "parent") {
        throw new Error("Создавать квесты может только родительский аккаунт. Выйдите и войдите как родитель.");
      }

      const child = await getChildProfile(childId);
      if (!child) {
        throw new Error("Ребенок не найден. Вернитесь на dashboard и выберите ребенка из списка.");
      }

      if (child.familyId !== profile.familyId) {
        throw new Error("Этот ребенок привязан к другой семье. Выберите ребенка из своего dashboard.");
      }

      await createQuest({ childId, title, description, category, rewardXp, rewardGold, createdByParentId: user.uid });
      router.push(`/children/${childId}/quests`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось создать квест");
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="card">
      <h1>Создать real-life квест</h1>
      <p>Квест появится у ребёнка в Unity, например в здании «Гильдия приключений».</p>
      <form className="grid" onSubmit={submit}>
        <label>Название<input value={title} onChange={(e) => setTitle(e.target.value)} required /></label>
        <label>Описание<textarea value={description} onChange={(e) => setDescription(e.target.value)} required /></label>
        <div className="grid2">
          <label>Категория<select value={category} onChange={(e) => setCategory(e.target.value as QuestCategory)}>{categories.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}</select></label>
          <label>XP<input type="number" min={0} value={rewardXp} onChange={(e) => setRewardXp(Number(e.target.value))} /></label>
          <label>Золото<input type="number" min={0} value={rewardGold} onChange={(e) => setRewardGold(Number(e.target.value))} /></label>
        </div>
        {error && <p className="error">{error}</p>}
        <button disabled={loading}>{loading ? "Создание..." : "Создать квест"}</button>
      </form>
    </section>
  );
}
