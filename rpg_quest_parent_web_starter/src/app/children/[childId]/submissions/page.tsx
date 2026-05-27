"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { QuestCard } from "@/components/QuestCard";
import { Quest } from "@/lib/types";
import { approveQuestDirect, listenChildQuests, rejectQuestDirect } from "@/lib/services/questService";

export default function SubmissionsPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [quests, setQuests] = useState<Quest[]>([]);
  const [rejectComment, setRejectComment] = useState("Попробуй ещё раз и добавь комментарий о выполнении.");

  useEffect(() => {
    return listenChildQuests(childId, setQuests, ["submitted"]);
  }, [childId]);

  return (
    <section className="grid">
      <div>
        <h1>Задания на проверке</h1>
        <p>В MVP одобрение пока обновляет статус напрямую. В production это лучше перенести в Cloud Functions.</p>
      </div>
      {quests.length === 0 ? <div className="card">Пока нет заданий на проверке.</div> : quests.map((quest) => (
        <QuestCard
          key={quest.id}
          quest={quest}
          actions={
            <>
              <button className="success" onClick={() => approveQuestDirect(childId, quest.id)}>Одобрить</button>
              <input value={rejectComment} onChange={(e) => setRejectComment(e.target.value)} />
              <button className="danger" onClick={() => rejectQuestDirect(childId, quest.id, rejectComment)}>Отклонить</button>
            </>
          }
        />
      ))}
    </section>
  );
}
