"use client";

import { useRouter } from "next/navigation";
import { useCurrentUser } from "@/components/current-user-provider";

export default function NavigationUser() {
  const router = useRouter();
  const { user, clearUser } = useCurrentUser();

  async function handleSignOut() {
    await fetch("/api/auth/session", {
      method: "DELETE",
      credentials: "include",
    });

    clearUser();
    router.replace("/login");
    router.refresh();
  }

  if (!user) {
    return null;
  }

  const initials = user.fullName
    .split(" ")
    .filter(Boolean)
    .map((word) => word[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <div className="app-nav-user">
      <div className="app-nav-avatar">{initials}</div>
      <span>{user.fullName}</span>
      <button type="button" className="app-sign-out" onClick={handleSignOut}>
        Sign out
      </button>
    </div>
  );
}
