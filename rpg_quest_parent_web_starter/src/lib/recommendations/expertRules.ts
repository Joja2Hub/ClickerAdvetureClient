import { ExpertRule } from "@/lib/recommendations/types";

export const expertRules: ExpertRule[] = [
  {
    id: "successful_category",
    title: "Успешная категория",
    apply: (f) => f.completionRate >= 0.75 && f.rejectionRate <= 0.15
      ? { ruleId: "successful_category", delta: 25, reason: "Ребенок стабильно выполняет задания этой категории." }
      : null,
  },
  {
    id: "fast_completion",
    title: "Быстрое выполнение",
    apply: (f) => f.avgSubmitHours !== null && f.avgSubmitHours <= 24
      ? { ruleId: "fast_completion", delta: 15, reason: "Похожие задания обычно выполняются быстро." }
      : null,
  },
  {
    id: "high_rejection_rate",
    title: "Высокая доля отклонений",
    apply: (f) => f.rejectionRate >= 0.35
      ? {
          ruleId: "high_rejection_rate",
          delta: -30,
          risk: "Задания этой категории часто требуют доработки.",
          action: "Упростить формулировку или уменьшить объем.",
        }
      : null,
  },
  {
    id: "low_view_to_submit",
    title: "Низкая конверсия просмотра",
    apply: (f) => f.viewedTotal >= 3 && f.viewToSubmitRate < 0.4
      ? {
          ruleId: "low_view_to_submit",
          delta: -20,
          risk: "Ребенок открывает задания, но редко отправляет их на проверку.",
          action: "Изменить награду или сделать задание короче.",
        }
      : null,
  },
  {
    id: "too_many_active_quests",
    title: "Высокая дневная нагрузка",
    apply: (f) => f.activeQuestCountToday >= 5
      ? {
          ruleId: "too_many_active_quests",
          delta: -25,
          risk: "На сегодня уже назначено много активных заданий.",
          action: "Перенести задание на другой день.",
        }
      : null,
  },
  {
    id: "category_fatigue",
    title: "Усталость от категории",
    apply: (f) => f.sameCategoryAssignedLast7Days >= 5 && f.completionRate < 0.6
      ? {
          ruleId: "category_fatigue",
          delta: -20,
          risk: "Возможна усталость от однотипных заданий.",
          action: "Сменить категорию.",
        }
      : null,
  },
  {
    id: "not_enough_data",
    title: "Недостаточно данных",
    apply: (f) => f.assignedTotal < 3
      ? {
          ruleId: "not_enough_data",
          delta: 0,
          reason: "По категории пока мало данных.",
          action: "Использовать пробное задание низкой сложности.",
        }
      : null,
  },
  {
    id: "reward_efficiency",
    title: "Эффективная награда",
    apply: (f) => f.completionRate >= 0.7 && f.avgRewardGold <= 50
      ? {
          ruleId: "reward_efficiency",
          delta: 10,
          reason: "Категория выполняется успешно без завышенной награды.",
        }
      : null,
  },
];
