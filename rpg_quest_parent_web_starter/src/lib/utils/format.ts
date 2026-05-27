export function formatQuestStatus(status: string): string {
  const map: Record<string, string> = {
    assigned: "Назначен",
    submitted: "На проверке",
    approved: "Одобрен",
    rejected: "Отклонён",
    claimed: "Награда получена",
    cancelled: "Отменён",
  };
  return map[status] ?? status;
}

export function formatCategory(category: string): string {
  const map: Record<string, string> = {
    study: "Учёба",
    home: "Дом",
    reading: "Чтение",
    health: "Здоровье",
    sport: "Спорт",
    hygiene: "Гигиена",
    family_help: "Помощь семье",
    custom: "Другое",
  };
  return map[category] ?? category;
}

export function todayId(date = new Date()): string {
  return date.toISOString().slice(0, 10);
}
