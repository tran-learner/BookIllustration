using System.Threading.Channels;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class PipelineJobQueue : IPipelineJobQueue
{
    private readonly Channel<PipelineJob> _jobs =
        Channel.CreateBounded<PipelineJob>(
            new BoundedChannelOptions(capacity: 10)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

    public ValueTask EnqueueAsync(
        PipelineJob job,
        CancellationToken cancellationToken = default) =>
        _jobs.Writer.WriteAsync(job, cancellationToken);

    public ValueTask<PipelineJob> DequeueAsync(
        CancellationToken cancellationToken) =>
        _jobs.Reader.ReadAsync(cancellationToken);
}
