import Link from "next/link";
import { ReactNode } from "react";
import { AuthGate } from "@/components/AuthGate";
import { TopNav } from "@/components/TopNav";

export default async function ChildLayout({ children, params }: { children: ReactNode; params: Promise<{ childId: string }> }) {
  const { childId } = await params;
  return (
    <AuthGate requireParent>
      <main className="page">
        <TopNav />
        <div className="tabs">
          <Link href={`/children/${childId}/quests`}>Квесты</Link>
          <Link href={`/children/${childId}/quests/new`}>Создать квест</Link>
          <Link href={`/children/${childId}/submissions`}>Проверка</Link>
          <Link href={`/children/${childId}/analytics`}>Аналитика</Link>
          <Link href={`/children/${childId}/login`}>Unity вход</Link>
        </div>
        {children}
      </main>
    </AuthGate>
  );
}
