"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function NavigationUser() {
  const router = useRouter();
  const [fullName, setFullName] = useState<string | null>(null);

  useEffect(() => {
    async function loadUser() {
      const response = await fetch("/api/auth/session", {
        credentials: "include",
      });

      if (response.ok) {
        const session = (await response.json()) as { fullName: string };
        setFullName(session.fullName);
      }
    }

    void loadUser();
  }, []);

  async function handleSignOut() {
    await fetch("/api/auth/session", {
      method: "DELETE",
      credentials: "include",
    });

    router.replace("/login");
    router.refresh();
  }

  if (!fullName) {
    return null;
  }

  const initials = fullName
    .split(" ")
    .filter(Boolean)
    .map((word) => word[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <div className="app-nav-user">
      <div className="app-nav-avatar">{initials}</div>
      <span>{fullName}</span>
      <button type="button" className="app-sign-out" onClick={handleSignOut}>
        Sign out
      </button>
    </div>
  );
}
