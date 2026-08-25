import type { PipelineStepResponse } from "@/types/project";
import ChapterStep from "./chapter-step";
import CharacterStep from "./character-step";
import IllustrationStep from "./illustration-step";
import PortraitStep from "./portrait-step";
import StyleStep from "./style-step";

type StepDetailProps = {
  step: PipelineStepResponse | null;
  projectId: number;
  onProjectUpdated: () => Promise<void>;
};

export default function StepDetail({
  step,
  projectId,
  onProjectUpdated,
}: StepDetailProps) {
  if (step == null) {
    return (
      <section className="step-content-placeholder">
        This project has completed every pipeline step.
      </section>
    );
  }

  switch (step.stepName) {
    case 1:
      return (
        <StyleStep
          step={step}
          projectId={projectId}
          onProjectUpdated={onProjectUpdated}
        />
      );
    case 2:
      return (
        <CharacterStep
          step={step}
          projectId={projectId}
          onProjectUpdated={onProjectUpdated}
        />
      );
    case 3:
      return (
        <PortraitStep
          step={step}
          projectId={projectId}
          onProjectUpdated={onProjectUpdated}
        />
      );
    case 4:
      return <ChapterStep step={step} />;
    case 5:
      return <IllustrationStep step={step} />;
    default:
      return (
        <section className="step-content-placeholder">
          The current pipeline step is not recognized.
        </section>
      );
  }
}
