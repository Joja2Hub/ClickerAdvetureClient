"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthProvider";
import { createFamily, createChild } from "@/lib/services/familyService";
import { createParentProfile } from "@/lib/services/userService";

export default function SetupPage() {
  const router = useRouter();
  const { user, loading: authLoading, refreshProfile } = useAuth();
  const [parentName, setParentName] = useState("");
  const [familyName, setFamilyName] = useState("Моя семья");
  const [childName, setChildName] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!user) return;
    setError("");
    setLoading(true);
    try {
      const familyId = await createFamily({ ownerParentId: user.uid, familyName });
      await createParentProfile({ uid: user.uid, email: user.email, displayName: parentName, familyId });
      await createChild({ familyId, displayName: childName });
      await refreshProfile();
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось создать профиль");
    } finally {
      setLoading(false);
    }
  }

  if (authLoading) {
    return <main className="centerPage"><section className="card narrow">Загрузка...</section></main>;
  }

  if (!user) {
    return (
      <main className="centerPage">
        <section className="card narrow">
          <h1>Сначала войдите</h1>
          <p>Для создания семьи нужен родительский аккаунт.</p>
          <Link className="button" href="/">На страницу входа</Link>
        </section>
      </main>
    );
  }

  return (
    <main className="centerPage">
      <section className="card narrow">
        <h1>Первичная настройка</h1>
        <p>Создадим семью и первого ребёнка. Позже можно добавить экран для нескольких детей.</p>
        <form className="grid" onSubmit={submit}>
          <label>Ваше имя<input value={parentName} onChange={(e) => setParentName(e.target.value)} required /></label>
          <label>Название семьи<input value={familyName} onChange={(e) => setFamilyName(e.target.value)} required /></label>
          <label>Имя ребёнка<input value={childName} onChange={(e) => setChildName(e.target.value)} required /></label>
          {error && <p className="error">{error}</p>}
          <button disabled={loading}>{loading ? "Создание..." : "Создать"}</button>
        </form>
      </section>
    </main>
  );
}
