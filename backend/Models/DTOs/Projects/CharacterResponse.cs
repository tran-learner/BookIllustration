namespace BookIllustration_Backend.Models.DTOs.Projects;

public class CharacterResponse
{
    public int CharacterId { get; set; }

    public required string CharacterName { get; set; }

    public required string CharacterDescription { get; set; }

    public bool HasPortrait { get; set; }
}
