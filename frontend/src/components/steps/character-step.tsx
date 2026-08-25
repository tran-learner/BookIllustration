import {
  pipelineStepStatusLabels,
  type PipelineStepResponse,
} from "@/types/project";

export default function CharacterStep({ step }: { step: PipelineStepResponse }) {
  return (
    <section className="step-panel">
      <div className="status-line">
        <strong>Characters</strong> is {pipelineStepStatusLabels[step.status] ?? "in an unknown state"}.
      </div>

      <p className="help">
        Reopening this page mid-step won&apos;t fire a second request — it just
        shows the same in-flight state until it lands.
      </p>

      <button type="button" className="gd-btn gd-btn-primary">
        Generate Characters <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
