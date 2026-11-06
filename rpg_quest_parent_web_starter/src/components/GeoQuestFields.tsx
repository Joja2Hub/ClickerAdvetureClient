import type { GeoQuestDraft } from "@/lib/geoQuest";

export function GeoQuestFields({ value, onChange, disabled }: {
  value: GeoQuestDraft;
  onChange: (value: GeoQuestDraft) => void;
  disabled: boolean;
}) {
  const set = (key: keyof GeoQuestDraft, next: string) => onChange({ ...value, [key]: next });
  return (
    <fieldset className="geoQuestFields grid" disabled={disabled}>
      <legend>Условия посещений</legend>
      <p>Укажите координаты места из карты. Засчитывается не более одного посещения за день в часовом поясе задания.</p>
      <div className="grid2">
        <label>Широта<input inputMode="decimal" placeholder="Например, 55.75" value={value.latitude} onChange={e => set("latitude", e.target.value)} required /></label>
        <label>Долгота<input inputMode="decimal" placeholder="Например, 37.62" value={value.longitude} onChange={e => set("longitude", e.target.value)} required /></label>
        <label>Радиус, метров<input type="number" min="1" step="1" value={value.radiusMeters} onChange={e => set("radiusMeters", e.target.value)} required /></label>
        <label>Число посещений<input type="number" min="1" step="1" value={value.requiredVisits} onChange={e => set("requiredVisits", e.target.value)} required /></label>
        <label>Первый день<input type="date" value={value.startDate} onChange={e => set("startDate", e.target.value)} required /></label>
        <label>Последний день включительно<input type="date" min={value.startDate} value={value.lastDate} onChange={e => set("lastDate", e.target.value)} required /></label>
      </div>
      <label>Часовой пояс задания<select value={value.utcOffsetMinutes} onChange={e => set("utcOffsetMinutes", e.target.value)}>
        {Array.from({ length: 27 }, (_, i) => i - 12).map(hour => <option key={hour} value={hour * 60}>UTC{hour >= 0 ? "+" : "−"}{String(Math.abs(hour)).padStart(2, "0")}:00{hour === 3 ? " — Москва" : ""}</option>)}
      </select></label>
      <p className="note">Посещения пока сохраняются на устройстве ребёнка. Автоматическая отправка геоквеста на проверку родителю появится на следующем этапе.</p>
    </fieldset>
  );
}
