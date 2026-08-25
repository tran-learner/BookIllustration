type CharacterPortraitCardItem = {
  id: string;
  name: string;
  description: string;
};

type CharacterPortraitCardsProps = {
  characters: CharacterPortraitCardItem[];
};

export default function CharacterPortraitCards({
  characters,
}: CharacterPortraitCardsProps) {
  return (
    <section className="generated-entities">
      <h3>Characters ({characters.length})</h3>

      <div className="entity-grid">
        {characters.map((character) => (
          <article className="entity-card" key={character.id}>
            <div className="entity-card-art character-art">
              <span>Portrait</span>
            </div>
            <div className="entity-card-body">
              <h5>{character.name}</h5>
              <p>{character.description}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
