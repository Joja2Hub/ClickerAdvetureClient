"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthProvider";

export default function LoginPage() {
  const router = useRouter();
  const { login, register } = useAuth();
  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      if (mode === "login") {
        await login(email, password);
        router.push("/dashboard");
      } else {
        await register(email, password);
        router.push("/setup");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось выполнить вход");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="centerPage">
      <section className="card narrow">
        <h1>{mode === "login" ? "Вход родителя" : "Регистрация родителя"}</h1>
        <p>Панель для создания real-life квестов, проверки выполнения и просмотра прогресса ребёнка.</p>
        <form onSubmit={submit} className="grid">
          <label>
            Email
            <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required />
          </label>
          <label>
            Пароль
            <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" minLength={6} required />
          </label>
          {error && <p className="error">{error}</p>}
          <button disabled={loading}>{loading ? "Подождите..." : mode === "login" ? "Войти" : "Создать аккаунт"}</button>
          <button className="secondary" type="button" onClick={() => setMode(mode === "login" ? "register" : "login")}>
            {mode === "login" ? "Создать новый аккаунт" : "Уже есть аккаунт"}
          </button>
        </form>
      </section>
    </main>
  );
}
