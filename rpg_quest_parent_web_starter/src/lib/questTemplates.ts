import { QuestCategory, QuestDifficulty } from "@/lib/types";

export type QuestTemplate = {
  id: string;
  title: string;
  description: string;
  category: QuestCategory;
  difficulty: QuestDifficulty;
};

export const questTemplates: QuestTemplate[] = [
  {
    id: "read-20",
    title: "Почитать 20 минут",
    description: "Прочитай книгу или учебный материал 20 минут и расскажи, что было самым интересным.",
    category: "reading",
    difficulty: "normal",
  },
  {
    id: "homework",
    title: "Сделать домашнее задание",
    description: "Выполни домашнее задание на завтра и проверь, что ничего не пропущено.",
    category: "study",
    difficulty: "hard",
  },
  {
    id: "tidy-room",
    title: "Навести порядок в комнате",
    description: "Убери вещи по местам, застели кровать и оставь рабочий стол чистым.",
    category: "home",
    difficulty: "normal",
  },
  {
    id: "wash-dishes",
    title: "Помочь с посудой",
    description: "Помой посуду после еды или аккуратно загрузи ее в посудомоечную машину.",
    category: "family_help",
    difficulty: "easy",
  },
  {
    id: "morning-routine",
    title: "Утренняя гигиена",
    description: "Умойся, почисти зубы и приведи себя в порядок без напоминаний.",
    category: "hygiene",
    difficulty: "easy",
  },
  {
    id: "exercise",
    title: "Сделать зарядку",
    description: "Выполни 15 минут разминки: приседания, наклоны, растяжку и упражнения на выбор.",
    category: "sport",
    difficulty: "normal",
  },
  {
    id: "walk",
    title: "Прогулка на свежем воздухе",
    description: "Проведи не менее 30 минут на свежем воздухе: прогуляйся, поиграй или покатайся.",
    category: "health",
    difficulty: "normal",
  },
  {
    id: "prepare-school",
    title: "Собрать вещи на завтра",
    description: "Проверь расписание и заранее собери рюкзак, одежду и все необходимое на завтра.",
    category: "study",
    difficulty: "easy",
  },
  {
    id: "family-help",
    title: "Помочь семье",
    description: "Спроси, какая помощь сейчас нужна дома, и самостоятельно выполни поручение.",
    category: "family_help",
    difficulty: "normal",
  },
  {
    id: "healthy-snack",
    title: "Выбрать полезный перекус",
    description: "Выбери полезный перекус и воду вместо сладостей или газированного напитка.",
    category: "health",
    difficulty: "easy",
  },
];

export const rewardsByDifficulty: Record<
  QuestDifficulty,
  { xp: number; gold: number }
> = {
  easy: { xp: 40, gold: 20 },
  normal: { xp: 80, gold: 40 },
  hard: { xp: 140, gold: 70 },
};
