"use client";

import { useState } from "react";
import {
  pipelineStepStatusLabels,
  type PipelineStepResponse,
} from "@/types/project";

type StyleStepProps = {
  step: PipelineStepResponse;
  projectId: number;
  onProjectUpdated: () => Promise<void>;
  nextStepLabel?: string;
  onContinue: () => void;
};

export default function StyleStep({
  step,
  projectId,
  onProjectUpdated,
  nextStepLabel,
  onContinue,
}: StyleStepProps) {
  const [style, setStyle] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isRunning = step.status === 1;
  const isFailed = step.status === 3;
  const isCompleted = step.status === 2;

  async function handleGenerateStyle() {
    setError("");
    setIsSubmitting(true);

    try {
      const response = await fetch(
        `/api/projects/${projectId}/pipeline/style`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            style: style.trim() || null,
          }),
        },
      );

      const payload = (await response.json().catch(() => null)) as {
        message?: string;
      } | null;

      if (response.status !== 202) {
        setError(
          payload?.message ?? "Unable to start Style generation. Please try again.",
        );
        return;
      }

      await onProjectUpdated();
    } catch {
      setError("Unable to reach the server. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="step-panel">
      <div className="status-line">
        {isRunning ? (
          <>
            <span className="spinner" aria-hidden="true" />
            Reading your book text and defining an art style.
          </>
        ) : (
          <>
            <strong>Style</strong> is {pipelineStepStatusLabels[step.status] ?? "in an unknown state"}.
          </>
        )}
      </div>

      <div className="gd-field style-step-field">
        <label htmlFor="style-input">Art style (optional)</label>
        <input
          id="style-input"
          value={style}
          onChange={(event) => setStyle(event.target.value)}
          placeholder="Leave blank to let Gemini choose a style based on your book"
        />
      </div>

      <p className="help">
        Reopening this page mid-step won&apos;t fire a second request — it just
        shows the same in-flight state until it lands.
      </p>

      {error && (
        <p className="form-error" aria-live="polite">
          {error}
        </p>
      )}

      {isCompleted && (
        <button type="button" className="continue-action" onClick={onContinue}>
          <span className="continue-action-label">Continue to {nextStepLabel}</span>
          <span aria-hidden="true">→</span>
        </button>
      )}

      <button
        type="button"
        className="gd-btn gd-btn-primary"
        onClick={handleGenerateStyle}
        disabled={isSubmitting || isRunning}
        hidden={isCompleted}
      >
        {isRunning && <span className="spinner" aria-hidden="true" />}
        {isFailed ? "Retry Style" : "Generate Style"} <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
