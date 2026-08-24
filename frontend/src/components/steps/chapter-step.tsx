export default function ChapterStep() {
  return (
    <section className="step-panel">
      <div className="status-line">
        Ready for the next step: <strong>Chapters</strong>.
      </div>

      <p className="help">
        Reopening this page mid-step won&apos;t fire a second request — it just
        shows the same in-flight state until it lands.
      </p>

      <button type="button" className="gd-btn gd-btn-primary">
        Generate Chapters <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
