"use client";

import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from "react";
import {
  User,
  createUserWithEmailAndPassword,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  signOut,
} from "firebase/auth";
import { auth } from "@/lib/firebase/client";
import { UserProfile } from "@/lib/types";
import { getUserProfile } from "@/lib/services/userService";
import { DEMO_PARENT_UID, DEMO_SESSION_KEY, demoParentProfile } from "@/lib/demoData";

interface AuthContextValue {
  user: User | null;
  profile: UserProfile | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  loginDemo: () => Promise<void>;
  register: (email: string, password: string) => Promise<User>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const demoUser = useMemo(
    () => ({
      uid: DEMO_PARENT_UID,
      email: demoParentProfile.email,
      displayName: demoParentProfile.displayName,
    }) as unknown as User,
    []
  );

  function setDemoSession() {
    setUser(demoUser);
    setProfile(demoParentProfile);
  }

  async function loadProfile(nextUser: User | null) {
    if (!nextUser) {
      setProfile(null);
      return;
    }
    const nextProfile = await getUserProfile(nextUser.uid);
    setProfile(nextProfile);
  }

  useEffect(() => {
    if (window.localStorage.getItem(DEMO_SESSION_KEY) === "1") {
      setDemoSession();
      setLoading(false);
      return;
    }

    const unsub = onAuthStateChanged(auth, async (nextUser) => {
      setUser(nextUser);
      await loadProfile(nextUser);
      setLoading(false);
    });
    return () => unsub();
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      profile,
      loading,
      async login(email, password) {
        window.localStorage.removeItem(DEMO_SESSION_KEY);
        const credential = await signInWithEmailAndPassword(auth, email, password);
        setUser(credential.user);
        await loadProfile(credential.user);
      },
      async loginDemo() {
        window.localStorage.setItem(DEMO_SESSION_KEY, "1");
        setDemoSession();
      },
      async register(email, password) {
        window.localStorage.removeItem(DEMO_SESSION_KEY);
        const credential = await createUserWithEmailAndPassword(auth, email, password);
        setUser(credential.user);
        return credential.user;
      },
      async logout() {
        window.localStorage.removeItem(DEMO_SESSION_KEY);
        await signOut(auth).catch(() => undefined);
        setUser(null);
        setProfile(null);
      },
      async refreshProfile() {
        await loadProfile(auth.currentUser);
      },
    }),
    [user, profile, loading]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
