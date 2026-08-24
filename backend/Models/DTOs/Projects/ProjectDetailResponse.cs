namespace BookIllustration_Backend.Models.DTOs.Projects;

public class ProjectDetailResponse
{
    public int ProjectId { get; set; }

    public required string ProjectTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Style { get; set; }

    public List<PipelineStepResponse> PipelineSteps { get; set; } = [];

    public List<CharacterResponse> Characters { get; set; } = [];

    public List<ChapterResponse> Chapters { get; set; } = [];
}
