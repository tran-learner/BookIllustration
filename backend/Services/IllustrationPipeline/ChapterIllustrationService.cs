using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.Configuration;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class ChapterIllustrationService(
    AppDbContext dbContext,
    GeminiClient geminiClient,
    IOptions<FileStorageOptions> fileStorageOptions)
{
    private static readonly JsonSerializerOptions StepDataJsonOptions =
        new(JsonSerializerDefaults.Web);

    private const int MaxChapterIllustrations = 1;

    private const string ChapterImageSetupInstruction =
        "Starting from now, we're going to illustrate the book's chapters. Don't forget to refer to your previous illustrations of the characters to keep the characters consistency, but feel free to change their position.";

    public async Task<Guid> ClaimIllustrationsStepAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        const int staleTimeoutMinutes = 5;

        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-staleTimeoutMinutes);

        var illustrationsStep = await dbContext.PipelineSteps
            .AsNoTracking()
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Illustrations,
                cancellationToken);

        if (illustrationsStep is null)
        {
            throw new InvalidOperationException("The Illustrations step was not found.");
        }

        if (illustrationsStep.Status == PipelineStepStatus.Completed)
        {
            throw new InvalidOperationException("The Illustrations step has already completed.");
        }

        if (illustrationsStep.Status == PipelineStepStatus.Running
            && illustrationsStep.UpdatedAt > staleThreshold)
        {
            throw new InvalidOperationException("The Illustrations step is already running.");
        }

        var runId = Guid.NewGuid();

        var claimedRows = await dbContext.PipelineSteps
            .Where(step => step.PipelineStepId == illustrationsStep.PipelineStepId
                && (step.Status == PipelineStepStatus.Pending
                    || step.Status == PipelineStepStatus.Failed
                    || (step.Status == PipelineStepStatus.Running
                        && step.UpdatedAt <= staleThreshold)))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(step => step.Status, PipelineStepStatus.Running)
                    .SetProperty(step => step.AttemptCount, step => step.AttemptCount + 1)
                    .SetProperty(step => step.RunId, (Guid?)runId)
                    .SetProperty(step => step.ErrorMessage, (string?)null)
                    .SetProperty(step => step.StartedAt, (DateTime?)now)
                    .SetProperty(step => step.UpdatedAt, now),
                cancellationToken);

        if (claimedRows == 0)
        {
            throw new InvalidOperationException(
                "The Illustrations step state changed before this request could start.");
        }

        return runId;
    }

    public async Task ExecuteIllustrationsStepAsync(
        int projectId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await SetIllustrationsAsync(projectId, runId, cancellationToken);
        }
        catch (Exception exception)
        {
            await dbContext.PipelineSteps
                .Where(step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Illustrations
                    && step.RunId == runId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(step => step.Status, PipelineStepStatus.Failed)
                        .SetProperty(step => step.ErrorMessage, exception.Message)
                        .SetProperty(step => step.UpdatedAt, DateTime.UtcNow),
                    CancellationToken.None);

            throw;
        }
    }

    public async Task SetIllustrationsAsync(
        int projectId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var illustrationsStep = await dbContext.PipelineSteps
            .Include(step => step.Project)
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Illustrations
                    && step.Status != PipelineStepStatus.Completed
                    && step.RunId == runId,
                cancellationToken);

        if (illustrationsStep is null)
        {
            throw new InvalidOperationException(
                "The Illustrations step was not found or has already completed.");
        }

        var stepData = string.IsNullOrWhiteSpace(illustrationsStep.StepData)
            ? new IllustrationStepData()
            : JsonSerializer.Deserialize<IllustrationStepData>(
                illustrationsStep.StepData,
                StepDataJsonOptions) ?? new IllustrationStepData();

        if (stepData.ChapterPrompts.Count == 0)
        {
            throw new InvalidOperationException(
                "The Illustrations step is missing its chapter prompts.");
        }

        if (stepData.ChapterPrompts.Count > MaxChapterIllustrations)
        {
            throw new InvalidOperationException(
                "The Illustrations step exceeds the maximum of one chapter.");
        }

        if (string.IsNullOrWhiteSpace(stepData.ImageInteractionId))
        {
            throw new InvalidOperationException(
                "The Illustrations step is missing its image interaction ID.");
        }

        if (string.IsNullOrWhiteSpace(stepData.ChapterImageInteractionId))
        {
            var chapterImageInteraction =
                await geminiClient.CreateImageInteractionContextAsync(
                    ChapterImageSetupInstruction,
                    cancellationToken,
                    previousInteractionId: stepData.ImageInteractionId);

            stepData.ChapterImageInteractionId = chapterImageInteraction.InteractionId;
            illustrationsStep.StepData = JsonSerializer.Serialize(
                stepData,
                StepDataJsonOptions);
            illustrationsStep.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await GenerateNeededChapterIllustrationsAsync(
            illustrationsStep,
            stepData,
            cancellationToken);

        var completedAt = DateTime.UtcNow;
        illustrationsStep.Status = PipelineStepStatus.Completed;
        illustrationsStep.CompletedAt = completedAt;
        illustrationsStep.UpdatedAt = completedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Each generated chapter illustration is checkpointed immediately: its file
    // path and the latest Gemini interaction ID are saved after every successful
    // image. A retry therefore generates only chapters still missing an image.
    private async Task GenerateNeededChapterIllustrationsAsync(
        PipelineStep illustrationsStep,
        IllustrationStepData stepData,
        CancellationToken cancellationToken)
    {
        var chapterNames = stepData.ChapterPrompts
            .Select(prompt => prompt.Name)
            .ToHashSet(StringComparer.Ordinal);

        var projectChapters = await dbContext.Chapters
            .Where(chapter => chapter.ProjectId == illustrationsStep.ProjectId
                && chapterNames.Contains(chapter.ChapterTitle))
            .ToListAsync(cancellationToken);

        var chaptersByName = projectChapters.ToDictionary(
            chapter => chapter.ChapterTitle,
            StringComparer.Ordinal);

        var missingChapter = stepData.ChapterPrompts
            .FirstOrDefault(prompt => !chaptersByName.ContainsKey(prompt.Name));

        if (missingChapter is not null)
        {
            throw new InvalidOperationException(
                $"The Illustrations step references an unknown chapter: {missingChapter.Name}.");
        }

        foreach (var chapterPrompt in stepData.ChapterPrompts)
        {
            var chapter = chaptersByName[chapterPrompt.Name];

            if (!string.IsNullOrWhiteSpace(chapter.ChapterIllustrationPath))
            {
                continue;
            }

            var imageInteraction = await geminiClient.CreateImageInteractionAsync(
                $"Create an illustration for {chapterPrompt.Name} using the previously generated characters following this description: {chapterPrompt.Prompt}",
                stepData.ChapterImageInteractionId,
                cancellationToken);

            var fileExtension = GetImageFileExtension(imageInteraction.MimeType);
            var illustrationsDirectory = Path.GetFullPath(
                fileStorageOptions.Value.IllustrationsDirectory);
            var filePath = Path.Combine(
                illustrationsDirectory,
                $"chapter{chapter.ChapterId}_book{illustrationsStep.ProjectId}{fileExtension}");

            Directory.CreateDirectory(illustrationsDirectory);
            await File.WriteAllBytesAsync(
                filePath,
                Convert.FromBase64String(imageInteraction.ImageData),
                cancellationToken);

            chapter.ChapterIllustrationPath = filePath;
            stepData.ChapterImageInteractionId = imageInteraction.InteractionId;
            illustrationsStep.StepData = JsonSerializer.Serialize(
                stepData,
                StepDataJsonOptions);
            illustrationsStep.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GetImageFileExtension(string mimeType) => mimeType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        _ => throw new InvalidOperationException(
            $"Gemini returned an unsupported image MIME type: {mimeType}.")
    };
}
