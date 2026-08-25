type ChapterIllustrationCardItem = {
  id: string;
  title: string;
  description: string;
};

type ChapterIllustrationCardsProps = {
  chapters: ChapterIllustrationCardItem[];
};

export default function ChapterIllustrationCards({
  chapters,
}: ChapterIllustrationCardsProps) {
  return (
    <section className="generated-entities">
      <h3>Chapters ({chapters.length})</h3>

      <div className="entity-grid chapter-entity-grid">
        {chapters.map((chapter) => (
          <article className="entity-card" key={chapter.id}>
            <div className="entity-card-art chapter-art">
              <span>Illustration</span>
            </div>
            <div className="entity-card-body">
              <h5>{chapter.title}</h5>
              <p>{chapter.description}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
