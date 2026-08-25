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
};

export default function CharacterStep({
  step,
  projectId,
  onProjectUpdated,
}: CharacterStepProps) {
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isRunning = step.status === 1;

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
        {isRunning && <span className="spinner" aria-hidden="true" />}
        <strong>Characters</strong> is {pipelineStepStatusLabels[step.status] ?? "in an unknown state"}.
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

      <button
        type="button"
        className="gd-btn gd-btn-primary"
        onClick={handleGenerateCharacters}
        disabled={isSubmitting || isRunning}
      >
        {isRunning && <span className="spinner" aria-hidden="true" />}
        Generate Characters <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
