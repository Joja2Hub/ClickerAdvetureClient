import type { GeoQuestConditions } from "./types";

export interface GeoQuestDraft {
  latitude: string;
  longitude: string;
  radiusMeters: string;
  requiredVisits: string;
  startDate: string;
  lastDate: string;
  utcOffsetMinutes: string;
}

export function newGeoQuestDraft(now = new Date()): GeoQuestDraft {
  const today = now.getTime() + 180 * 60_000;
  return {
    latitude: "", longitude: "", radiusMeters: "100", requiredVisits: "5",
    startDate: new Date(today).toISOString().slice(0, 10),
    lastDate: new Date(today + 6 * 86_400_000).toISOString().slice(0, 10),
    utcOffsetMinutes: "180",
  };
}

function numeric(value: string, label: string): number {
  if (!value.trim()) throw new Error(`Заполните поле «${label}».`);
  const number = Number(value.trim().replace(",", "."));
  if (!Number.isFinite(number)) throw new Error(`Проверьте поле «${label}».`);
  return number;
}

function midnight(value: string): number {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) throw new Error("Укажите даты начала и окончания.");
  const date = new Date(`${value}T00:00:00Z`);
  if (!Number.isFinite(date.getTime()) || date.toISOString().slice(0, 10) !== value)
    throw new Error("Проверьте даты геоквеста.");
  return date.getTime() / 1000;
}

export function validateGeoQuest(geo: GeoQuestConditions): void {
  if (geo.version !== 1) throw new Error("Неизвестная версия условий геоквеста.");
  if (!Number.isFinite(geo.latitude) || geo.latitude < -90 || geo.latitude > 90)
    throw new Error("Широта должна быть от −90 до 90.");
  if (!Number.isFinite(geo.longitude) || geo.longitude < -180 || geo.longitude > 180)
    throw new Error("Долгота должна быть от −180 до 180.");
  if (!Number.isFinite(geo.radiusMeters) || geo.radiusMeters <= 0)
    throw new Error("Радиус должен быть больше нуля.");
  if (!Number.isInteger(geo.requiredVisits) || geo.requiredVisits < 1 || geo.requiredVisits > 2_147_483_647)
    throw new Error("Число посещений должно быть положительным целым числом.");
  if (!Number.isInteger(geo.utcOffsetMinutes) || Math.abs(geo.utcOffsetMinutes) > 840)
    throw new Error("Проверьте часовой пояс задания.");
  for (const time of [geo.startsAtUnixSeconds, geo.endsAtUnixSeconds]) {
    const local = time + geo.utcOffsetMinutes * 60;
    if (!Number.isInteger(time) || time < -62_135_596_800 || time > 253_402_300_799
      || local < -62_135_596_800 || local > 253_402_300_799)
      throw new Error("Проверьте срок геоквеста.");
  }
  if (geo.endsAtUnixSeconds <= geo.startsAtUnixSeconds)
    throw new Error("Окончание геоквеста должно быть после начала.");
  const firstDay = Math.floor((geo.startsAtUnixSeconds + geo.utcOffsetMinutes * 60) / 86_400);
  const lastDay = Math.ceil((geo.endsAtUnixSeconds + geo.utcOffsetMinutes * 60) / 86_400) - 1;
  if (geo.requiredVisits > lastDay - firstDay + 1)
    throw new Error("В выбранном сроке недостаточно дней: засчитывается одно посещение в день.");
}

export function buildGeoQuest(draft: GeoQuestDraft): GeoQuestConditions {
  const offset = numeric(draft.utcOffsetMinutes, "Часовой пояс");
  const geo: GeoQuestConditions = {
    version: 1,
    latitude: numeric(draft.latitude, "Широта"),
    longitude: numeric(draft.longitude, "Долгота"),
    radiusMeters: numeric(draft.radiusMeters, "Радиус"),
    requiredVisits: numeric(draft.requiredVisits, "Посещения"),
    startsAtUnixSeconds: midnight(draft.startDate) - offset * 60,
    endsAtUnixSeconds: midnight(draft.lastDate) + 86_400 - offset * 60,
    utcOffsetMinutes: offset,
  };
  validateGeoQuest(geo);
  return geo;
}
