namespace BookIllustration_Backend.Models.DTOs.Pipeline;

public class ChapterStepData
{
    public string? CharacterInteractionId { get; set; }

    public string? ImageInteractionId { get; set; }

    public string? ChapterInteractionId { get; set; }

    public List<ChapterPrompt> ChapterPrompts { get; set; } = [];
}
