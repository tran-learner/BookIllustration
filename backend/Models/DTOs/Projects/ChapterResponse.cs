namespace BookIllustration_Backend.Models.DTOs.Projects;

public class ChapterResponse
{
    public int ChapterId { get; set; }

    public required string ChapterTitle { get; set; }

    public required string ChapterDescription { get; set; }

    public bool HasIllustration { get; set; }
}
