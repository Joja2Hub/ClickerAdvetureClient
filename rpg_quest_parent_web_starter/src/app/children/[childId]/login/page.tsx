"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { getChildProfile } from "@/lib/services/childService";
import { createChildLogin } from "@/lib/services/childLoginService";
import { ChildProfile } from "@/lib/types";

export default function ChildLoginPage() {
  const params = useParams<{ childId: string }>();
  const childId = params.childId;
  const [child, setChild] = useState<ChildProfile | null>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [status, setStatus] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    getChildProfile(childId).then((nextChild) => {
      setChild(nextChild);
      setDisplayName(nextChild?.displayName || "");
      setEmail(nextChild?.unityLoginEmail || "");
      setPassword(nextChild?.unityLoginPasswordForTesting || "");
    });
  }, [childId]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setStatus("Создаем вход для Unity...");
    try {
      await createChildLogin({ childId, email, password, displayName });
      const nextChild = await getChildProfile(childId);
      setChild(nextChild);
      setStatus("Готово. Данные для входа сохранены.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Не удалось создать вход.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="stack">
      <h1>Настройка аккаунта Unity</h1>
      <section className="card">
        <h2>Данные для входа ребенка</h2>
        {child?.unityLoginEmail ? (
          <div className="grid2">
            <label>
              Email
              <input value={child.unityLoginEmail} readOnly />
            </label>
            <label>
              Пароль
              <input value={child.unityLoginPasswordForTesting || "Пароль не сохранен"} readOnly />
            </label>
          </div>
        ) : (
          <p>Для этого ребенка Unity-вход еще не создан.</p>
        )}
      </section>
      <form className="card form" onSubmit={onSubmit}>
        <label>
          Имя героя
          <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} placeholder="Юный герой" />
        </label>
        <label>
          Email ребенка
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
        <button disabled={busy}>{busy ? "Сохраняем..." : "Сохранить Unity-вход"}</button>
      </form>
      {status ? <div className="card">{status}</div> : null}
    </main>
  );
}
