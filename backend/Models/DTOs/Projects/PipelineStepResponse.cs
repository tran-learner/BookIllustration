using BookIllustration_Backend.Models.Entities;

namespace BookIllustration_Backend.Models.DTOs.Projects;

public class PipelineStepResponse
{
    public Guid PipelineStepId { get; set; }

    public PipelineStepName StepName { get; set; }

    public PipelineStepStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public string? StepData { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
