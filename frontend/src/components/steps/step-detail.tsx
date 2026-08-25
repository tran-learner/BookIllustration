import type {
  ChapterResponse,
  CharacterResponse,
  PipelineStepResponse,
} from "@/types/project";
import ChapterIllustrationCards from "@/components/entities/chapter-illustration-cards";
import CharacterPortraitCards from "@/components/entities/character-portrait-cards";
import ChapterStep from "./chapter-step";
import CharacterStep from "./character-step";
import IllustrationStep from "./illustration-step";
import PortraitStep from "./portrait-step";
import StyleStep from "./style-step";

type StepDetailProps = {
  step: PipelineStepResponse | null;
  projectId: number;
  characters: CharacterResponse[];
  chapters: ChapterResponse[];
  onProjectUpdated: () => Promise<void>;
  onContinueToNextStep: () => void;
};

export default function StepDetail({
  step,
  projectId,
  characters,
  chapters,
  onProjectUpdated,
  onContinueToNextStep,
}: StepDetailProps) {
  const portraitCharacters = characters.filter(
    (character) => character.hasPortrait,
  );
  const illustratedChapters = chapters.filter(
    (chapter) => chapter.hasIllustration,
  );

  const isFinalStepComplete = step?.stepName === 5 && step.status === 2;
  const nextStepLabel = step ? {
    1: "Characters",
    2: "Portraits",
    3: "Chapters",
    4: "Illustrations",
  }[step.stepName] : undefined;

  let stepContent;

  if (step == null || isFinalStepComplete) {
    stepContent = (
      <section className="step-content-placeholder">
        This project has completed every pipeline step.
      </section>
    );
  } else {
    switch (step.stepName) {
      case 1:
        stepContent = (
        <StyleStep
          step={step}
          projectId={projectId}
          onProjectUpdated={onProjectUpdated}
          nextStepLabel={nextStepLabel}
          onContinue={onContinueToNextStep}
        />
        );
        break;
      case 2:
        stepContent = (
        <CharacterStep
          step={step}
          projectId={projectId}
          onProjectUpdated={onProjectUpdated}
          nextStepLabel={nextStepLabel}
          onContinue={onContinueToNextStep}
        />
        );
        break;
      case 3:
        stepContent = (
        <PortraitStep
          step={step}
          projectId={projectId}
          onProjectUpdated={onProjectUpdated}
          nextStepLabel={nextStepLabel}
          onContinue={onContinueToNextStep}
        />
        );
        break;
      case 4:
        stepContent = (
          <ChapterStep
            step={step}
            projectId={projectId}
            onProjectUpdated={onProjectUpdated}
            nextStepLabel={nextStepLabel}
            onContinue={onContinueToNextStep}
          />
        );
        break;
      case 5:
        stepContent = (
          <IllustrationStep
            step={step}
            projectId={projectId}
            onProjectUpdated={onProjectUpdated}
            nextStepLabel={nextStepLabel}
            onContinue={onContinueToNextStep}
          />
        );
        break;
      default:
        stepContent = (
        <section className="step-content-placeholder">
          The current pipeline step is not recognized.
        </section>
        );
        break;
    }
  }

  return (
    <>
      {stepContent}
      {illustratedChapters.length > 0 && (
        <ChapterIllustrationCards
          projectId={projectId}
          chapters={illustratedChapters}
        />
      )}
      {portraitCharacters.length > 0 && (
        <CharacterPortraitCards
          projectId={projectId}
          characters={portraitCharacters}
        />
      )}
    </>
  );
}
