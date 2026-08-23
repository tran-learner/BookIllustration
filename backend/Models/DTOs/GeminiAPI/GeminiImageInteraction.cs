namespace BookIllustration_Backend.Models.DTOs.GeminiAPI;

public class GeminiImageInteraction
{
    public required string InteractionId { get; set; }

    public required string ImageData { get; set; }

    public required string MimeType { get; set; }
}
