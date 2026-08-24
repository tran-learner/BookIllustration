namespace BookIllustration_Backend.Models.DTOs.Authentication;

public class AuthSession
{
    public required string AccessToken { get; set; }

    public DateTime ExpiresAt { get; set; }
}
