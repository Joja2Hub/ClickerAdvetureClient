import { QuestStatus } from "@/lib/types";
import { formatQuestStatus } from "@/lib/utils/format";

export function QuestStatusBadge({ status }: { status: QuestStatus }) {
  return <span className={`badge status-${status}`}>{formatQuestStatus(status)}</span>;
}
