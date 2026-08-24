namespace BookIllustration_Backend.Services.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";

    public string Audience { get; set; } = "";

    public int ExpirationMinutes { get; set; }

    public string SigningKey { get; set; } = "";
}
