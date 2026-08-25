import {
  pipelineStepStatusLabels,
  type PipelineStepResponse,
} from "@/types/project";

export default function IllustrationStep({ step }: { step: PipelineStepResponse }) {
  const isRunning = step.status === 1;

  return (
    <section className="step-panel">
      <div className="status-line">
        {isRunning && <span className="spinner" aria-hidden="true" />}
        <strong>Illustrations</strong> is {pipelineStepStatusLabels[step.status] ?? "in an unknown state"}.
      </div>

      <p className="help">
        Reopening this page mid-step won&apos;t fire a second request — it just
        shows the same in-flight state until it lands.
      </p>

      <button type="button" className="gd-btn gd-btn-primary" disabled={isRunning}>
        {isRunning && <span className="spinner" aria-hidden="true" />}
        Generate Illustrations <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
