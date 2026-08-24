export default function PortraitStep() {
  return (
    <section className="step-panel">
      <div className="status-line">
        Ready for the next step: <strong>Portraits</strong>.
      </div>

      <p className="help">
        Reopening this page mid-step won&apos;t fire a second request — it just
        shows the same in-flight state until it lands.
      </p>

      <button type="button" className="gd-btn gd-btn-primary">
        Generate Portraits <span aria-hidden="true">→</span>
      </button>
    </section>
  );
}
