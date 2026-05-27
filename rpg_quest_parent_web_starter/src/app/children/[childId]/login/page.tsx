"use client";

import { FormEvent, useState } from "react";
import { useParams } from "next/navigation";
import { createChildLogin } from "@/lib/services/childLoginService";

export default function ChildLoginPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [status, setStatus] = useState("");
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setStatus("Создаём вход для Unity...");
    try {
      const uid = await createChildLogin({ childId, email, password, displayName });
      setStatus(`Готово. UID ребёнка: ${uid}`);
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Не удалось создать вход.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="stack">
      <h1>Вход ребёнка в Unity</h1>
      <form className="card form" onSubmit={onSubmit}>
        <label>
          Имя героя
          <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} placeholder="Юный герой" />
        </label>
        <label>
          Email ребёнка
          <input value={email} onChange={(event) => setEmail(event.target.value)} placeholder="child@example.com" required />
        </label>
        <label>
          Пароль
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            minLength={6}
            required
          />
        </label>
        <button disabled={busy}>{busy ? "Сохраняем..." : "Создать вход"}</button>
      </form>
      {status ? <div className="card">{status}</div> : null}
    </main>
  );
}
