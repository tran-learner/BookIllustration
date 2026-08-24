"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

export default function NewProjectPage() {
  const router = useRouter();
  const [projectTitle, setProjectTitle] = useState("");
  const [bookText, setBookText] = useState("");
  const [bookFile, setBookFile] = useState<File | null>(null);
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleCreateProject() {
    if (!projectTitle.trim()) {
      setError("Project title is required.");
      return;
    }

    if (!bookFile && !bookText.trim()) {
      setError("Provide a .txt file or paste the book text.");
      return;
    }

    const fileToUpload =
      bookFile ??
      new File([bookText], "book.txt", {
        type: "text/plain",
      });

    if (!bookFile) {
      setBookFile(fileToUpload);
    }

    setError("");
    setIsSubmitting(true);

    try {
      const formData = new FormData();
      formData.append("projectTitle", projectTitle.trim());
      formData.append("bookFile", fileToUpload);

      const response = await fetch("/api/projects", {
        method: "POST",
        credentials: "include",
        body: formData,
      });

      const payload = (await response.json().catch(() => null)) as {
        projectId?: number;
        message?: string;
      } | null;

      if (!response.ok) {
        setError(
          payload?.message ?? "Unable to create the project. Please try again.",
        );
        return;
      }

      if (!payload?.projectId) {
        setError("The server returned an invalid project response.");
        return;
      }

      router.push(`/projects/${payload.projectId}`);
    } catch {
      setError("Unable to reach the server. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="app-body app-body-narrow">
      <Link href="/projects" className="back-link">
        ← Back to projects
      </Link>

      <h1 className="new-project-title">Start a new illustration project</h1>
      <p className="new-project-description">
        Give it a title, then paste the book&apos;s text or upload a .txt file.
      </p>

      <div className="gd-field">
        <label htmlFor="project-title">
          Project title <span aria-hidden="true">*</span>
        </label>
        <input
          id="project-title"
          name="projectTitle"
          value={projectTitle}
          onChange={(event) => setProjectTitle(event.target.value)}
          placeholder="e.g. The Wind in the Willows — cottage-core"
        />
      </div>

      <div className="gd-field new-project-book-field">
        <label htmlFor="book-text">
          Book text <span aria-hidden="true">*</span>
        </label>

        <label htmlFor="book-file" className="dropzone">
          <span className="dropzone-label">Click to choose a .txt file</span>
          <span className="dropzone-hint">
            Plain text only · used once as context for every step below
          </span>
        </label>
        <input
          id="book-file"
          type="file"
          accept=".txt"
          hidden
          onChange={(event) => setBookFile(event.target.files?.[0] ?? null)}
        />

        <div className="divider-or">or paste text</div>

        <textarea
          id="book-text"
          name="bookText"
          rows={5}
          value={bookText}
          onChange={(event) => setBookText(event.target.value)}
          placeholder="Once upon a time, in a small burrow by the river..."
        />
      </div>

      <p className="form-error" aria-live="polite">
        {error}
      </p>

      <button
        type="button"
        className="gd-btn gd-btn-primary new-project-submit"
        onClick={handleCreateProject}
        disabled={isSubmitting}
      >
        {isSubmitting ? "Creating…" : "Create project"}
        {!isSubmitting && <span aria-hidden="true">→</span>}
      </button>
    </div>
  );
}
