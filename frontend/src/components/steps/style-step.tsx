import {
  pipelineStepStatusLabels,
  type PipelineStepResponse,
} from "@/types/project";

export default function StyleStep({ step }: { step: PipelineStepResponse }) {
  return (
    <section className="step-panel">
      <div className="status-line">
        <strong>Style</strong> is {pipelineStepStatusLabels[step.status] ?? "in an unknown state"}.
      </div>

      <div className="gd-field style-step-field">
        <label htmlFor="style-input">Art style (optional)</label>
        <input
          id="style-input"
          placeholder="Leave blank to let Gemini choose a style based on your book"
        />
      </div>

      <p className="help">
        Reopening this page mid-step won&apos;t fire a second request — it just
        shows the same in-flight state until it lands.
      </p>

      <button type="button" className="gd-btn gd-btn-primary">
        Generate Style <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
