namespace BookIllustration_Backend.Services.IllustrationPipeline;

public interface IPipelineJobQueue
{
    ValueTask EnqueueAsync(
        PipelineJob job,
        CancellationToken cancellationToken = default);

    ValueTask<PipelineJob> DequeueAsync(
        CancellationToken cancellationToken);
}
