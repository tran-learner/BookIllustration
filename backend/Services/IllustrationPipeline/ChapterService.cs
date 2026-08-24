using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.EntityFrameworkCore;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class ChapterService(AppDbContext dbContext, GeminiClient geminiClient)
{
    private static readonly JsonSerializerOptions StepDataJsonOptions =
        new(JsonSerializerDefaults.Web);

    private const int MaxChapters = 1;

    private const string ChapterPromptInstruction =
        "Now, for each chapters of the book, give me a prompt to illustrate what happens in it. It should be a single image, not a multi-tiled page. Be very descriptive, especially of the characters. Be very descriptive and remember to tell their name and to reuse the character prompts if they appear in the images. Also list all characters who appear in it.";

    private static readonly object ChapterResponseFormat = new
    {
        type = "text",
        mime_type = "application/json",
        schema = new
        {
            type = "array",
            maxItems = MaxChapters,
            items = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string" },
                    prompt = new { type = "string" }
                },
                required = new[] { "name", "prompt" }
            }
        }
    };

    public async Task RunChapterStepAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        const int staleTimeoutMinutes = 5;

        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-staleTimeoutMinutes);

        var chapterStep = await dbContext.PipelineSteps
            .AsNoTracking()
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Chapters,
                cancellationToken);

        if (chapterStep is null)
        {
            throw new InvalidOperationException("The Chapters step was not found.");
        }

        if (chapterStep.Status == PipelineStepStatus.Completed)
        {
            throw new InvalidOperationException("The Chapters step has already completed.");
        }

        if (chapterStep.Status == PipelineStepStatus.Running
            && chapterStep.UpdatedAt > staleThreshold)
        {
            throw new InvalidOperationException("The Chapters step is already running.");
        }

        var runId = Guid.NewGuid();

        var claimedRows = await dbContext.PipelineSteps
            .Where(step => step.PipelineStepId == chapterStep.PipelineStepId
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
                "The Chapters step state changed before this request could start.");
        }

        try
        {
            await SetChaptersAsync(projectId, runId, cancellationToken);
        }
        catch (Exception exception)
        {
            await dbContext.PipelineSteps
                .Where(step => step.PipelineStepId == chapterStep.PipelineStepId
                    && step.RunId == runId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(step => step.Status, PipelineStepStatus.Failed)
                        .SetProperty(step => step.ErrorMessage, exception.Message)
                        .SetProperty(step => step.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            throw;
        }
    }

    public async Task SetChaptersAsync(
        int projectId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var chapterStep = await dbContext.PipelineSteps
            .Include(step => step.Project)
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Chapters
                    && step.Status != PipelineStepStatus.Completed
                    && step.RunId == runId,
                cancellationToken);

        if (chapterStep is null)
        {
            throw new InvalidOperationException(
                "The Chapters step was not found or has already completed.");
        }

        var stepData = string.IsNullOrWhiteSpace(chapterStep.StepData)
            ? new ChapterStepData()
            : JsonSerializer.Deserialize<ChapterStepData>(
                chapterStep.StepData,
                StepDataJsonOptions) ?? new ChapterStepData();

        if (string.IsNullOrWhiteSpace(stepData.CharacterInteractionId))
        {
            throw new InvalidOperationException(
                "The Chapters step is missing its Character interaction ID.");
        }

        if (!string.IsNullOrWhiteSpace(stepData.ChapterInteractionId))
        {
            throw new InvalidOperationException(
                "The Chapters step has an interaction ID but did not complete.");
        }

        var chapterInteraction = await geminiClient.CreateTextInteractionAsync(
            ChapterPromptInstruction,
            stepData.CharacterInteractionId,
            cancellationToken,
            responseFormat: ChapterResponseFormat);

        if (string.IsNullOrWhiteSpace(chapterInteraction.Text))
        {
            throw new InvalidOperationException("Gemini returned an empty chapter response.");
        }

        var chapterPrompts = JsonSerializer.Deserialize<List<ChapterPrompt>>(
            chapterInteraction.Text,
            StepDataJsonOptions);

        if (chapterPrompts is null || chapterPrompts.Count == 0
            || chapterPrompts.Any(chapter =>
                string.IsNullOrWhiteSpace(chapter.Name)
                || string.IsNullOrWhiteSpace(chapter.Prompt)))
        {
            throw new InvalidOperationException(
                "Gemini returned an invalid chapter response.");
        }

        chapterPrompts = chapterPrompts.Take(MaxChapters).ToList();
        stepData.ChapterInteractionId = chapterInteraction.InteractionId;
        stepData.ChapterPrompts = chapterPrompts;
        chapterStep.StepData = JsonSerializer.Serialize(stepData, StepDataJsonOptions);

        foreach (var chapterPrompt in chapterPrompts)
        {
            dbContext.Chapters.Add(new Chapter
            {
                ChapterTitle = chapterPrompt.Name,
                ChapterDescription = chapterPrompt.Prompt,
                ProjectId = chapterStep.ProjectId,
                Project = chapterStep.Project
            });
        }

        var completedAt = DateTime.UtcNow;
        chapterStep.Status = PipelineStepStatus.Completed;
        chapterStep.CompletedAt = completedAt;
        chapterStep.UpdatedAt = completedAt;

        dbContext.PipelineSteps.Add(new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Illustrations,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new IllustrationStepData
                {
                    ImageInteractionId = stepData.ImageInteractionId,
                    ChapterPrompts = stepData.ChapterPrompts
                },
                StepDataJsonOptions),
            UpdatedAt = chapterStep.UpdatedAt.AddTicks(1),
            ProjectId = chapterStep.ProjectId,
            Project = chapterStep.Project
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
