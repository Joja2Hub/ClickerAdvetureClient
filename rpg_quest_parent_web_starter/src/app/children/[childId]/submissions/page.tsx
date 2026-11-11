"use client";

import { useParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { QuestCard } from "@/components/QuestCard";
import { Quest } from "@/lib/types";
import { approveQuest, listenChildQuests, rejectQuest } from "@/lib/services/questService";
import { isDemoChild } from "@/lib/demoData";

export default function SubmissionsPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [quests, setQuests] = useState<Quest[]>([]);
  const [comments, setComments] = useState<Record<string, string>>({});
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [error, setError] = useState("");
  const saving = useRef(false);

  useEffect(() => {
    return listenChildQuests(childId, setQuests, ["submitted"]);
  }, [childId]);

  async function review(quest: Quest, approve: boolean) {
    if (saving.current) return;
    saving.current = true;
    setPendingId(quest.id);
    setError("");
    try {
      if (approve) await approveQuest(childId, quest.id);
      else await rejectQuest(childId, quest.id, comments[quest.id]?.trim() || "Попробуй ещё раз.");
    } catch (err) {
      const message = err instanceof Error ? err.message : "";
      setError(message ? `Не удалось сохранить решение. ${message}` : "Не удалось сохранить решение. Проверьте связь и повторите попытку.");
    } finally {
      saving.current = false;
      setPendingId(null);
    }
  }

  return (
    <section className="grid">
      <div>
        <h1>Задания на проверке</h1>
        <p>Проверьте выполнение задания и выберите, принять результат или вернуть его на доработку.</p>
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      {isDemoChild(childId) && <p>В демо можно посмотреть задания на проверке.</p>}
      {quests.length === 0 ? <div className="card">Пока нет заданий на проверке.</div> : quests.map((quest) => (
        <QuestCard
          key={quest.id}
          quest={quest}
          actions={
            <>
              <button className="success" disabled={pendingId !== null || isDemoChild(childId)} onClick={() => void review(quest, true)}>Одобрить</button>
              <input aria-label="Комментарий к отклонению" placeholder="Комментарий к отклонению"
                value={comments[quest.id] ?? ""} disabled={pendingId !== null || isDemoChild(childId)}
                onChange={e => setComments(current => ({ ...current, [quest.id]: e.target.value }))} />
              <button className="danger" disabled={pendingId !== null || isDemoChild(childId)} onClick={() => void review(quest, false)}>Отклонить</button>
              {pendingId === quest.id && <p role="status">Сохраняем решение…</p>}
            </>
          }
        />
      ))}
    </section>
  );
}
