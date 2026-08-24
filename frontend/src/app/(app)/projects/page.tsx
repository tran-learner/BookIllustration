"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

type PipelineStepResponse = {
  pipelineStepId: string;
  stepName: number;
  status: number;
  attemptCount: number;
  stepData: string | null;
  startedAt: string | null;
  updatedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
};

type ProjectListItemResponse = {
  projectId: number;
  projectTitle: string;
  createdAt: string;
  style: string | null;
  completedStepCount: number;
  latestPipelineStep: PipelineStepResponse | null;
};

const stepLabels = [
  "Style",
  "Characters",
  "Portraits",
  "Chapters",
  "Illustrations",
];

const projectProgressText = [
  "Book text saved · style not yet generated",
  "Style recorded · characters not yet described",
  "Characters described · portraits not yet generated",
  "Portraits generated · chapters not yet described",
  "Chapters described · illustrations not yet generated",
  "All 5 steps complete",
];

function EmptyProjectState() {
  return (
    <section className="empty-state">
      <p>No projects yet.</p>
      <Link href="/projects/new" className="gd-btn gd-btn-primary">
        + New project
      </Link>
    </section>
  );
}

function getCompletedStepCount(project: ProjectListItemResponse) {
  return Math.min(
    Math.max(project.completedStepCount, 0),
    stepLabels.length,
  );
}

function ProjectSubtitle({ project }: { project: ProjectListItemResponse }) {
  const completedStepCount = getCompletedStepCount(project);

  return projectProgressText[completedStepCount];
}

function ProjectStatusPill({ project }: { project: ProjectListItemResponse }) {
  const completedStepCount = getCompletedStepCount(project);

  if (completedStepCount === 0) {
    return <span className="gd-pill gd-pill-gray">Draft</span>;
  }

  if (completedStepCount === stepLabels.length) {
    return <span className="gd-pill gd-pill-ink">Done</span>;
  }

  return (
    <span className="gd-pill">
      <span className="gd-pill-dot" />
      In progress
    </span>
  );
}

function ProjectList({ projects }: { projects: ProjectListItemResponse[] }) {
  return (
    <section className="project-list">
      {projects.map((project) => (
        <Link key={project.projectId} href={`/projects/${project.projectId}`} className="project-row">
          <div className="project-row-title">
            <h2>{project.projectTitle}</h2>
            <span className="project-meta">
              Created {new Date(project.createdAt).toLocaleDateString()} ·{" "}
              <ProjectSubtitle project={project} />
            </span>
          </div>

          <div className="progress-mini" aria-label="Project progress">
            {stepLabels.map((step, index) => (
              <span
                key={step}
                className={`progress-segment ${
                  index < getCompletedStepCount(project) ? "is-complete" : ""
                }`}
              />
            ))}
          </div>

          <ProjectStatusPill project={project} />
        </Link>
      ))}
    </section>
  );
}

export default function ProjectsPage() {
  const [projects, setProjects] = useState<ProjectListItemResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadProjects() {
      try {
        const response = await fetch("/api/projects", {
          credentials: "include",
        });

        if (!response.ok) {
          setError("Unable to load projects. Please try again.");
          return;
        }

        const data = (await response.json()) as ProjectListItemResponse[];
        setProjects(data);
      } catch {
        setError("Unable to reach the server. Please try again.");
      } finally {
        setIsLoading(false);
      }
    }

    void loadProjects();
  }, []);

  return (
    <div className="app-body">
      <header className="list-head">
        <h1>Your projects</h1>
        <Link href="/projects/new" className="gd-btn gd-btn-primary">
          + New project
        </Link>
      </header>

      {isLoading ? (
        <p className="project-list-message">Loading projects…</p>
      ) : error ? (
        <p className="project-list-message form-error">{error}</p>
      ) : projects.length === 0 ? (
        <EmptyProjectState />
      ) : (
        <ProjectList projects={projects} />
      )}
    </div>
  );
}
