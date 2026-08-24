namespace BookIllustration_Backend.Models.DTOs.Projects;

public class ProjectListItemResponse
{
    public int ProjectId { get; set; }

    public required string ProjectTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Style { get; set; }

    public int CompletedStepCount { get; set; }

    public PipelineStepResponse? LatestPipelineStep { get; set; }
}
