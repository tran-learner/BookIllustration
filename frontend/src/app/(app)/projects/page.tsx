import Link from "next/link";

type ProjectSummary = {
  projectId: string;
  title: string;
};

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

function ProjectListPlaceholder({ projects }: { projects: ProjectSummary[] }) {
  return (
    <section className="project-list-placeholder">
      Project list placeholder ({projects.length} projects)
    </section>
  );
}

export default function ProjectsPage() {
  // This will be replaced with project data loaded from the API.
  const projects: ProjectSummary[] = [];

  return (
    <div className="app-body">
      <header className="list-head">
        <h1>Your projects</h1>
        <Link href="/projects/new" className="gd-btn gd-btn-primary">
          + New project
        </Link>
      </header>

      {projects.length === 0 ? (
        <EmptyProjectState />
      ) : (
        <ProjectListPlaceholder projects={projects} />
      )}
    </div>
  );
}
