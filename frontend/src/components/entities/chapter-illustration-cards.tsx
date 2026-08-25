import type { ChapterResponse } from "@/types/project";

type ChapterIllustrationCardsProps = {
  projectId: number;
  chapters: ChapterResponse[];
};

export default function ChapterIllustrationCards({
  projectId,
  chapters,
}: ChapterIllustrationCardsProps) {
  return (
    <section className="generated-entities">
      <h3>Chapters ({chapters.length})</h3>

      <div className="entity-grid chapter-entity-grid">
        {chapters.map((chapter) => (
          <article className="entity-card" key={chapter.chapterId}>
            {/* The browser calls the authenticated same-origin stream endpoint directly. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              className="entity-card-art entity-card-image chapter-art"
              src={`/api/projects/${projectId}/chapters/${chapter.chapterId}/illustration`}
              alt={`Illustration for ${chapter.chapterTitle}`}
            />
            <div className="entity-card-body">
              <h5>{chapter.chapterTitle}</h5>
              <p>{chapter.chapterDescription}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
