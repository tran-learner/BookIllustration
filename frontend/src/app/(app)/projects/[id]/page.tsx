"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { useCurrentUser } from "@/components/current-user-provider";
import StepDetail from "@/components/steps/step-detail";
import type { PipelineStepResponse } from "@/types/project";

type CharacterResponse = {
  characterId: number;
  characterName: string;
  characterDescription: string;
};

type ChapterResponse = {
  chapterId: number;
  chapterTitle: string;
  chapterDescription: string;
};

type ProjectDetailResponse = {
  projectId: number;
  projectTitle: string;
  createdAt: string;
  style: string | null;
  pipelineSteps: PipelineStepResponse[];
  characters: CharacterResponse[];
  chapters: ChapterResponse[];
};

const stepDefinitions = [
  { stepName: 1, label: "Style" },
  { stepName: 2, label: "Characters" },
  { stepName: 3, label: "Portraits" },
  { stepName: 4, label: "Chapters" },
  { stepName: 5, label: "Illustrations" },
];

const completedStatus = 2;

function ProjectStepper({ pipelineSteps }: { pipelineSteps: PipelineStepResponse[] }) {
  const firstIncompleteIndex = stepDefinitions.findIndex((definition) => {
    const step = pipelineSteps.find(
      (pipelineStep) => pipelineStep.stepName === definition.stepName,
    );

    return step?.status !== completedStatus;
  });

  return (
    <div className="stepper" aria-label="Project progress">
      {stepDefinitions.map((definition, index) => {
        const step = pipelineSteps.find(
          (pipelineStep) => pipelineStep.stepName === definition.stepName,
        );
        const isDone = step?.status === completedStatus;
        const isCurrent = index === firstIncompleteIndex;
        const stepClass = isDone ? "is-done" : isCurrent ? "is-current" : "is-pending";

        return (
          <div className="stepper-item" key={definition.stepName}>
            <div className={`stepper-step ${stepClass}`}>
              <span className="stepper-number">{isDone ? "✓" : index + 1}</span>
              <span className="stepper-label">{definition.label}</span>
            </div>
            {index < stepDefinitions.length - 1 && (
              <span className={`stepper-connector ${isDone ? "is-done" : ""}`} />
            )}
          </div>
        );
      })}
    </div>
  );
}

function ProjectSideNote({
  projectId,
  style,
}: {
  projectId: number;
  style: string | null;
}) {
  const [bookText, setBookText] = useState<string | null>(null);
  const [isBookFileTooLarge, setIsBookFileTooLarge] = useState(false);
  const [bookTextError, setBookTextError] = useState("");

  useEffect(() => {
    setBookText(null);
    setIsBookFileTooLarge(false);
    setBookTextError("");

    if (style) {
      return;
    }

    let isActive = true;

    async function loadBookText() {
      try {
        const response = await fetch(`/api/projects/${projectId}/book-text`, {
          credentials: "include",
        });

        if (!isActive) {
          return;
        }

        if (response.status === 413) {
          setIsBookFileTooLarge(true);
          return;
        }

        if (!response.ok) {
          setBookTextError("Unable to load book text.");
          return;
        }

        const text = await response.text();

        if (isActive) {
          setBookText(text);
        }
      } catch {
        if (isActive) {
          setBookTextError("Unable to load book text.");
        }
      }
    }

    void loadBookText();

    return () => {
      isActive = false;
    };
  }, [projectId, style]);

  if (style) {
    return (
      <aside className="side-note">
        <h2>Style</h2>
        <p>{style}</p>
      </aside>
    );
  }

  return (
    <aside className="side-note">
      <h2>Book text</h2>
      {isBookFileTooLarge ? (
        <p>The file is too large to display.</p>
      ) : bookTextError ? (
        <p>{bookTextError}</p>
      ) : bookText ? (
        <p className="side-note-book-text">{bookText}</p>
      ) : (
        <p>Loading book text…</p>
      )}
    </aside>
  );
}

export default function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useCurrentUser();
  const [project, setProject] = useState<ProjectDetailResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const projectId = Number(id);

    if (!Number.isInteger(projectId) || projectId <= 0) {
      setError("The project ID is invalid.");
      setIsLoading(false);
      return;
    }

    async function loadProject() {
      try {
        const response = await fetch(`/api/projects/${projectId}`, {
          credentials: "include",
        });

        if (response.status === 404) {
          setError("The project was not found.");
          return;
        }

        if (!response.ok) {
          setError("Unable to load the project. Please try again.");
          return;
        }

        const data = (await response.json()) as ProjectDetailResponse;
        console.log("Project detail response:", data);
        setProject(data);
      } catch {
        setError("Unable to reach the server. Please try again.");
      } finally {
        setIsLoading(false);
      }
    }

    void loadProject();
  }, [id]);

  if (isLoading) {
    return <p className="project-detail-output">Loading project…</p>;
  }

  if (error || !project) {
    return <p className="project-detail-output form-error">{error}</p>;
  }

  const currentStep = project.pipelineSteps.find(
    (step) => step.status !== completedStatus,
  ) ?? null;

  return (
    <div className="app-body">
      <Link href="/projects" className="back-link">
        ← Back to projects
      </Link>

      <h1 className="project-detail-title">{project.projectTitle}</h1>
      <p className="project-detail-meta">
        Created {new Date(project.createdAt).toLocaleDateString()} by{" "}
        {user?.fullName ?? "…"}
      </p>

      <ProjectStepper pipelineSteps={project.pipelineSteps} />

      <div className="project-detail-grid">
        <div>
          <StepDetail step={currentStep} />
        </div>

        <ProjectSideNote projectId={project.projectId} style={project.style} />
      </div>
    </div>
  );
}
