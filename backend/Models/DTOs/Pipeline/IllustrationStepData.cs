namespace BookIllustration_Backend.Models.DTOs.Pipeline;

public class IllustrationStepData
{
    public string? ImageInteractionId { get; set; }

    public string? ChapterImageInteractionId { get; set; }

    public List<ChapterPrompt> ChapterPrompts { get; set; } = [];
}
