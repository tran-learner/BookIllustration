"use client";

import { useState } from "react";
import {
  pipelineStepStatusLabels,
  type PipelineStepResponse,
} from "@/types/project";

type CharacterStepProps = {
  step: PipelineStepResponse;
  projectId: number;
  onProjectUpdated: () => Promise<void>;
  nextStepLabel?: string;
  onContinue: () => void;
};

export default function CharacterStep({
  step,
  projectId,
  onProjectUpdated,
  nextStepLabel,
  onContinue,
}: CharacterStepProps) {
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isRunning = step.status === 1;
  const isFailed = step.status === 3;
  const isCompleted = step.status === 2;

  async function handleGenerateCharacters() {
    setError("");
    setIsSubmitting(true);

    try {
      const response = await fetch(
        `/api/projects/${projectId}/pipeline/characters`,
        {
          method: "POST",
          credentials: "include",
        },
      );

      const payload = (await response.json().catch(() => null)) as {
        message?: string;
      } | null;

      if (response.status !== 202) {
        setError(
          payload?.message ?? "Unable to start Character generation. Please try again.",
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
            Generating the character list from your book&apos;s text.
          </>
        ) : (
          <>
            <strong>Characters</strong> is {pipelineStepStatusLabels[step.status] ?? "in an unknown state"}.
          </>
        )}
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
          Continue to {nextStepLabel} <span aria-hidden="true">→</span>
        </button>
      )}

      <button
        type="button"
        className="gd-btn gd-btn-primary"
        onClick={handleGenerateCharacters}
        disabled={isSubmitting || isRunning}
        hidden={isCompleted}
      >
        {isRunning && <span className="spinner" aria-hidden="true" />}
        {isFailed ? "Retry Characters" : "Generate Characters"} <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
