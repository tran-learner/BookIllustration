namespace BookIllustration_Backend.Services.GeminiFeatures;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string BaseUrl { get; set; } = string.Empty;

    public string TextModel { get; set; } = string.Empty;

    public string ImageModel { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
