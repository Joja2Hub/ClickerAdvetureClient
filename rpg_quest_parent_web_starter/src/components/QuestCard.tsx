import { Quest } from "@/lib/types";
import { formatCategory } from "@/lib/utils/format";
import { QuestStatusBadge } from "@/components/QuestStatusBadge";

export function QuestCard({ quest, actions }: { quest: Quest; actions?: React.ReactNode }) {
  return (
    <article className="questCard">
      <div className="questHeader">
        <div>
          <h3>{quest.title}</h3>
          <p>{formatCategory(quest.category)}</p>
        </div>
        <QuestStatusBadge status={quest.status} />
      </div>
      <p>{quest.description}</p>
      {quest.childComment && <p className="note"><b>Комментарий ребёнка:</b> {quest.childComment}</p>}
      {quest.parentComment && <p className="note"><b>Комментарий родителя:</b> {quest.parentComment}</p>}
      <div className="rewardRow">
        <span>+{quest.rewardXp} XP</span>
        <span>+{quest.rewardGold} золота</span>
      </div>
      {actions && <div className="actions">{actions}</div>}
    </article>
  );
}
