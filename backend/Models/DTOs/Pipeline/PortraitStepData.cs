namespace BookIllustration_Backend.Models.DTOs.Pipeline;

public class PortraitStepData
{
    public string? CharacterInteractionId { get; set; }

    public string? ImageInteractionId { get; set; }

    public List<CharacterPrompt> CharacterPrompts { get; set; } = [];
}
