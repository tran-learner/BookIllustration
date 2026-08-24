using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.EntityFrameworkCore;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class StyleService(AppDbContext dbContext, GeminiClient geminiClient)
{
    private static readonly JsonSerializerOptions StepDataJsonOptions =
        new(JsonSerializerDefaults.Web);

    private const string BookInteractionInstruction =
        "Here's a book, to illustrate using Nano Banana. Don't say anything for now, instructions will follow.";

    private const string StyleInstruction =
        "Can you define a art style that would fit the story but with a twist? Just give us the prompt for the art syle that will added to the furture prompts.";

    // Claim the Style step for one user-triggered attempt before any Gemini call.
    public async Task RunStyleStepAsync(
        int projectId,
        string? userProvidedStyle,
        CancellationToken cancellationToken = default)
    {
        const int staleTimeoutMinutes = 5;

        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-staleTimeoutMinutes);

        var styleStep = await dbContext.PipelineSteps
            .AsNoTracking()
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Style,
                cancellationToken);

        if (styleStep is null)
        {
            throw new InvalidOperationException("The Style step was not found.");
        }

        if (styleStep.Status == PipelineStepStatus.Completed)
        {
            throw new InvalidOperationException("The Style step has already completed.");
        }

        if (styleStep.Status == PipelineStepStatus.Running
            && styleStep.UpdatedAt > staleThreshold)
        {
            throw new InvalidOperationException("The Style step is already running.");
        }

        var runId = Guid.NewGuid();

        var claimedRows = await dbContext.PipelineSteps
            .Where(step => step.PipelineStepId == styleStep.PipelineStepId
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
                "The Style step state changed before this request could start.");
        }

        try
        {
            await SetStyleAsync(
                projectId,
                userProvidedStyle,
                runId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await dbContext.PipelineSteps
                .Where(step => step.PipelineStepId == styleStep.PipelineStepId
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

    // Reuse persisted Gemini context whenever possible:
    // 1. If a book interaction already exists, create or record the style from it.
    // 2. Otherwise, upload the local book file only when no uploaded file URI exists.
    // 3. Create and persist the book interaction, then use its ID for the style interaction.
    public async Task SetStyleAsync(
        int projectId,
        string? userProvidedStyle,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var styleStep = await dbContext.PipelineSteps
            .Include(step => step.Project)
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Style
                    && step.Status != PipelineStepStatus.Completed
                    && step.RunId == runId,
                cancellationToken);

        if (styleStep is null)
        {
            throw new InvalidOperationException(
                "The Style step was not found or has already completed.");
        }

        var styleStepData = string.IsNullOrWhiteSpace(styleStep.StepData)
            ? new StyleStepData()
            : JsonSerializer.Deserialize<StyleStepData>(
                styleStep.StepData,
                StepDataJsonOptions) ?? new StyleStepData();

        if (!string.IsNullOrWhiteSpace(styleStepData.StyleInteractionId))
        {
            if (string.IsNullOrWhiteSpace(styleStepData.UploadedFileUri)
                || string.IsNullOrWhiteSpace(styleStepData.BookInteractionId))
            {
                throw new InvalidOperationException(
                    "The Style step has a style interaction ID but its earlier Gemini context is incomplete.");
            }

            styleStep.Status = PipelineStepStatus.Completed;
            styleStep.CompletedAt ??= DateTime.UtcNow;
            styleStep.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        if (string.IsNullOrWhiteSpace(styleStepData.BookInteractionId))
        {
            if (string.IsNullOrWhiteSpace(styleStepData.UploadedFileUri))
            {
                await using var bookFile = File.OpenRead(styleStep.Project.BookTextPath);

                var uploadedBook = await geminiClient.UploadFileAsync(
                    bookFile,
                    bookFile.Length,
                    "text/plain",
                    Path.GetFileName(styleStep.Project.BookTextPath),
                    cancellationToken);

                styleStepData.UploadedFileUri = uploadedBook.Uri;
                styleStep.StepData = JsonSerializer.Serialize(
                    styleStepData,
                    StepDataJsonOptions);
                styleStep.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var bookInteraction = await geminiClient.CreateBookInteractionAsync(
                styleStepData.UploadedFileUri!,
                BookInteractionInstruction,
                cancellationToken);

            styleStepData.BookInteractionId = bookInteraction.InteractionId;
            styleStep.StepData = JsonSerializer.Serialize(
                styleStepData,
                StepDataJsonOptions);
            styleStep.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var styleInstruction = string.IsNullOrWhiteSpace(userProvidedStyle)
            ? StyleInstruction
            : $"The art style will be:\"{userProvidedStyle}\". Keep that in mind when generating future prompts. Keep quiet for now, instructions will follow.";

        var styleInteraction = await geminiClient.CreateTextInteractionAsync(
            styleInstruction,
            styleStepData.BookInteractionId,
            cancellationToken);

        styleStepData.StyleInteractionId = styleInteraction.InteractionId;
        styleStep.StepData = JsonSerializer.Serialize(
            styleStepData,
            StepDataJsonOptions);

        styleStep.Project.Style = string.IsNullOrWhiteSpace(userProvidedStyle)
            ? styleInteraction.Text
                ?? throw new InvalidOperationException("Gemini returned an empty style.")
            : userProvidedStyle;

        styleStep.Status = PipelineStepStatus.Completed;
        styleStep.CompletedAt = DateTime.UtcNow;
        styleStep.UpdatedAt = DateTime.UtcNow;

        dbContext.PipelineSteps.Add(new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Characters,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new CharacterStepData
                {
                    StyleInteractionId = styleInteraction.InteractionId
                },
                StepDataJsonOptions),
            UpdatedAt = styleStep.UpdatedAt.AddTicks(1),
            ProjectId = styleStep.ProjectId,
            Project = styleStep.Project
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
