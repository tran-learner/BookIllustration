namespace BookIllustration_Backend.Models.DTOs.Authentication;

public class CreateSessionRequest
{
    public required string Email { get; set; }

    public required string FullName { get; set; }
}
