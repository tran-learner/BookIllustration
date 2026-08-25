using BookIllustration_Backend.Models.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class PipelineWorker(
    IPipelineJobQueue jobQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<PipelineWorker> logger) : BackgroundService
{
    private const int MaxConcurrentJobs = 2;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(
            Enumerable.Range(0, MaxConcurrentJobs)
                .Select(_ => ConsumeJobsAsync(stoppingToken)));

    private async Task ConsumeJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            PipelineJob job;

            try
            {
                job = await jobQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();

                switch (job.StepName)
                {
                    case PipelineStepName.Style:
                        await scope.ServiceProvider
                            .GetRequiredService<StyleService>()
                            .ExecuteStyleStepAsync(
                                job.ProjectId,
                                job.RunId,
                                stoppingToken);
                        break;
                    case PipelineStepName.Characters:
                        await scope.ServiceProvider
                            .GetRequiredService<CharacterService>()
                            .ExecuteCharacterStepAsync(
                                job.ProjectId,
                                job.RunId,
                                stoppingToken);
                        break;
                    case PipelineStepName.Portraits:
                        await scope.ServiceProvider
                            .GetRequiredService<PortraitService>()
                            .ExecutePortraitStepAsync(
                                job.ProjectId,
                                job.RunId,
                                stoppingToken);
                        break;
                    case PipelineStepName.Chapters:
                        await scope.ServiceProvider
                            .GetRequiredService<ChapterService>()
                            .ExecuteChapterStepAsync(
                                job.ProjectId,
                                job.RunId,
                                stoppingToken);
                        break;
                    case PipelineStepName.Illustrations:
                        await scope.ServiceProvider
                            .GetRequiredService<ChapterIllustrationService>()
                            .ExecuteIllustrationsStepAsync(
                                job.ProjectId,
                                job.RunId,
                                stoppingToken);
                        break;
                    default:
                        logger.LogWarning(
                            "No background worker handler exists for pipeline step {StepName}.",
                            job.StepName);
                        break;
                }
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Pipeline job failed for project {ProjectId}, step {StepName}, run {RunId}.",
                    job.ProjectId,
                    job.StepName,
                    job.RunId);
            }
        }
    }
}
