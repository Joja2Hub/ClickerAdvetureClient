"use client";

import Link from "next/link";
import { ReactNode } from "react";
import { useAuth } from "@/context/AuthProvider";

export function AuthGate({ children }: { children: ReactNode }) {
  const { user, profile, loading } = useAuth();

  if (loading) return <main className="page"><div className="card">Загрузка...</div></main>;

  if (!user) {
    return (
      <main className="page">
        <div className="card narrow">
          <h1>Нужен вход</h1>
          <p>Войдите в родительский аккаунт, чтобы управлять квестами.</p>
          <Link className="button" href="/">На страницу входа</Link>
        </div>
      </main>
    );
  }

  if (!profile) {
    return (
      <main className="page">
        <div className="card narrow">
          <h1>Профиль ещё не создан</h1>
          <p>Создайте семейный профиль и первого ребёнка.</p>
          <Link className="button" href="/setup">Настроить</Link>
        </div>
      </main>
    );
  }

  return <>{children}</>;
}
