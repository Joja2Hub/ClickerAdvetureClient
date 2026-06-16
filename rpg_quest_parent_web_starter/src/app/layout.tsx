import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/context/AuthProvider";

export const metadata: Metadata = {
  title: "RPG Quest Parent Dashboard",
  description: "Родительская панель для управления заданиями и прогрессом ребёнка.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="ru">
      <body>
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
