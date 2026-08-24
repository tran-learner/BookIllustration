"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";

export default function LoginPage() {
  const router = useRouter();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      const response = await fetch("/api/auth/session", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ fullName, email }),
      });

      const payload = (await response.json().catch(() => null)) as {
        message?: string;
      } | null;

      if (!response.ok) {
        setError(payload?.message ?? "Unable to sign in. Please try again.");
        return;
      }

      router.push("/projects");
    } catch {
      setError("Unable to reach the server. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="center-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <div className="auth-logo-row">
          <Image
            src="/gradion-logo.png"
            alt="Gradion"
            width={110}
            height={26}
            priority
          />
        </div>

        <h1>Book Illustration Studio</h1>
        <p className="auth-lede">
          Enter your details to start or resume an illustration project.
        </p>

        <div className="gd-field">
          <label htmlFor="full-name">
            Full name <span aria-hidden="true">*</span>
          </label>
          <input
            id="full-name"
            name="fullName"
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            placeholder="Mira Hassan"
            required
          />
        </div>

        <div className="gd-field">
          <label htmlFor="email">
            Email <span aria-hidden="true">*</span>
          </label>
          <input
            id="email"
            name="email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="mira@example.com"
            required
          />
        </div>

        <p className="form-error" aria-live="polite">
          {error}
        </p>

        <button
          type="submit"
          className="gd-btn gd-btn-primary auth-submit"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Continuing…" : "Continue"}
          {!isSubmitting && <span aria-hidden="true">→</span>}
        </button>

        <p className="auth-note">
          No password — this is a lightweight identity check. Using an email
          that already has projects resumes them exactly where you left off.
        </p>
      </form>
    </section>
  );
}
