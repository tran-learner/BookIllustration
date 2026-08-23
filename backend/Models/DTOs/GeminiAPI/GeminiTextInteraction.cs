namespace BookIllustration_Backend.Models.DTOs.GeminiAPI;

public class GeminiTextInteraction
{
    public required string InteractionId { get; set; }

    public string? Text { get; set; }
}
