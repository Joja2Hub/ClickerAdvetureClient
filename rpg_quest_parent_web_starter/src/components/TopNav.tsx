"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthProvider";

export function TopNav() {
  const { profile, logout } = useAuth();
  const router = useRouter();

  return (
    <header className="topnav">
      <Link href="/dashboard" className="brand">RPG Quest Parent</Link>
      <nav>
        <Link href="/dashboard">Дети</Link>
        <button
          className="linkButton"
          onClick={async () => {
            await logout();
            router.push("/");
          }}
        >
          Выйти{profile?.displayName ? ` (${profile.displayName})` : ""}
        </button>
      </nav>
    </header>
  );
}
