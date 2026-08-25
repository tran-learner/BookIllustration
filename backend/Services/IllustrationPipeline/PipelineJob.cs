using BookIllustration_Backend.Models.Entities;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public record PipelineJob(
    int ProjectId,
    PipelineStepName StepName,
    Guid RunId);
