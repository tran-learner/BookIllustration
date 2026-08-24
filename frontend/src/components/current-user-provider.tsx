"use client";

import { createContext, useContext, useEffect, useState } from "react";

type CurrentUser = {
  fullName: string;
};

type CurrentUserContextValue = {
  user: CurrentUser | null;
  clearUser: () => void;
};

const CurrentUserContext = createContext<CurrentUserContextValue | null>(null);

export function CurrentUserProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    async function loadUser() {
      const response = await fetch("/api/auth/session", {
        credentials: "include",
      });

      if (response.ok) {
        setUser((await response.json()) as CurrentUser);
      }
    }

    void loadUser();
  }, []);

  return (
    <CurrentUserContext.Provider
      value={{ user, clearUser: () => setUser(null) }}
    >
      {children}
    </CurrentUserContext.Provider>
  );
}

export function useCurrentUser() {
  const context = useContext(CurrentUserContext);

  if (!context) {
    throw new Error("useCurrentUser must be used within CurrentUserProvider.");
  }

  return context;
}
