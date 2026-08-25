import type { CharacterResponse } from "@/types/project";

type CharacterPortraitCardsProps = {
  projectId: number;
  characters: CharacterResponse[];
};

export default function CharacterPortraitCards({
  projectId,
  characters,
}: CharacterPortraitCardsProps) {
  return (
    <section className="generated-entities">
      <h3>Characters ({characters.length})</h3>

      <div className="entity-grid">
        {characters.map((character) => (
          <article className="entity-card" key={character.characterId}>
            {/* The browser calls the authenticated same-origin stream endpoint directly. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              className="entity-card-art entity-card-image character-art"
              src={`/api/projects/${projectId}/characters/${character.characterId}/portrait`}
              alt={`Portrait of ${character.characterName}`}
            />
            <div className="entity-card-body">
              <h5>{character.characterName}</h5>
              <p>{character.characterDescription}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
