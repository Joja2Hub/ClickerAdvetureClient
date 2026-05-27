"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { QuestCard } from "@/components/QuestCard";
import { Quest } from "@/lib/types";
import { cancelQuestDirect, listenChildQuests } from "@/lib/services/questService";

export default function ChildQuestsPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [quests, setQuests] = useState<Quest[]>([]);

  useEffect(() => {
    return listenChildQuests(childId, setQuests);
  }, [childId]);

  return (
    <section className="grid">
      <div className="toolbar">
        <div>
          <h1>Квесты ребёнка</h1>
          <p>Здесь видны все задания: назначенные, отправленные, одобренные и завершённые.</p>
        </div>
        <Link className="button" href={`/children/${childId}/quests/new`}>Создать квест</Link>
      </div>
      {quests.length === 0 ? <div className="card">Квестов пока нет.</div> : quests.map((quest) => (
        <QuestCard
          key={quest.id}
          quest={quest}
          actions={quest.status === "assigned" ? (
            <button className="secondary" onClick={() => cancelQuestDirect(childId, quest.id)}>Отменить</button>
          ) : null}
        />
      ))}
    </section>
  );
}
