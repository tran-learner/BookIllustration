using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookIllustration_Backend.Models.Entities;

public class PipelineStep
{
    [Key]
    public Guid PipelineStepId { get; set; }

    public PipelineStepName StepName { get; set; }

    public PipelineStepStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public string? StepData { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    [ConcurrencyCheck]
    public Guid? RunId { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public required Project Project { get; set; }
}
