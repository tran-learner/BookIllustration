using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.Configuration;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class PortraitService(
    AppDbContext dbContext,
    GeminiClient geminiClient,
    IOptions<FileStorageOptions> fileStorageOptions)
{
    private static readonly JsonSerializerOptions StepDataJsonOptions =
        new(JsonSerializerDefaults.Web);

    private const int MaxCharacterPortraits = 2;

    private const string SystemInstructions = "";

    public async Task RunPortraitStepAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        const int staleTimeoutMinutes = 5;

        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-staleTimeoutMinutes);

        var portraitStep = await dbContext.PipelineSteps
            .AsNoTracking()
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Portraits,
                cancellationToken);

        if (portraitStep is null)
        {
            throw new InvalidOperationException("The Portraits step was not found.");
        }

        if (portraitStep.Status == PipelineStepStatus.Completed)
        {
            throw new InvalidOperationException("The Portraits step has already completed.");
        }

        if (portraitStep.Status == PipelineStepStatus.Running
            && portraitStep.UpdatedAt > staleThreshold)
        {
            throw new InvalidOperationException("The Portraits step is already running.");
        }

        var runId = Guid.NewGuid();

        var claimedRows = await dbContext.PipelineSteps
            .Where(step => step.PipelineStepId == portraitStep.PipelineStepId
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
                "The Portraits step state changed before this request could start.");
        }

        try
        {
            await SetPortraitsAsync(projectId, runId, cancellationToken);
        }
        catch (Exception exception)
        {
            await dbContext.PipelineSteps
                .Where(step => step.PipelineStepId == portraitStep.PipelineStepId
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

    public async Task SetPortraitsAsync(
        int projectId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var portraitStep = await dbContext.PipelineSteps
            .Include(step => step.Project)
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Portraits
                    && step.Status != PipelineStepStatus.Completed
                    && step.RunId == runId,
                cancellationToken);

        if (portraitStep is null)
        {
            throw new InvalidOperationException(
                "The Portraits step was not found or has already completed.");
        }

        var stepData = string.IsNullOrWhiteSpace(portraitStep.StepData)
            ? new PortraitStepData()
            : JsonSerializer.Deserialize<PortraitStepData>(
                portraitStep.StepData,
                StepDataJsonOptions) ?? new PortraitStepData();

        if (stepData.CharacterPrompts.Count == 0)
        {
            throw new InvalidOperationException(
                "The Portraits step is missing its character prompts.");
        }

        if (stepData.CharacterPrompts.Count > MaxCharacterPortraits)
        {
            throw new InvalidOperationException(
                "The Portraits step exceeds the maximum of two characters.");
        }

        if (string.IsNullOrWhiteSpace(portraitStep.Project.Style))
        {
            throw new InvalidOperationException(
                "The Portraits step requires a project style.");
        }

        if (string.IsNullOrWhiteSpace(stepData.ImageInteractionId))
        {
            var imageInteraction = await geminiClient.CreateImageInteractionContextAsync(
                $"""
                You are going to generate portrait images to illustrate The Wind in the Willows from Kenneth Grahame.
                The style we want you to follow is: {portraitStep.Project.Style}
                Also follow those rules: {SystemInstructions} # TODO: System instructions
                """,
                cancellationToken);

            stepData.ImageInteractionId = imageInteraction.InteractionId;
            portraitStep.StepData = JsonSerializer.Serialize(stepData, StepDataJsonOptions);
            portraitStep.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await GenerateNeededPortraitsAsync(
            portraitStep,
            stepData,
            cancellationToken);

        var completedAt = DateTime.UtcNow;
        portraitStep.Status = PipelineStepStatus.Completed;
        portraitStep.CompletedAt = completedAt;
        portraitStep.UpdatedAt = completedAt;

        dbContext.PipelineSteps.Add(new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Chapters,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new ChapterStepData
                {
                    CharacterInteractionId = stepData.CharacterInteractionId,
                    ImageInteractionId = stepData.ImageInteractionId
                },
                StepDataJsonOptions),
            UpdatedAt = portraitStep.UpdatedAt.AddTicks(1),
            ProjectId = portraitStep.ProjectId,
            Project = portraitStep.Project
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Each generated portrait is checkpointed immediately: its file path and the
    // latest Gemini interaction ID are saved after every successful image.
    // If a later portrait fails, a retry skips characters that already have an
    // illustration path and generates only the remaining portraits.
    private async Task GenerateNeededPortraitsAsync(
        PipelineStep portraitStep,
        PortraitStepData stepData,
        CancellationToken cancellationToken)
    {
        var characterNames = stepData.CharacterPrompts
            .Select(prompt => prompt.Name)
            .ToHashSet(StringComparer.Ordinal);

        var projectCharacters = await dbContext.Characters
            .Where(character => character.ProjectId == portraitStep.ProjectId
                && characterNames.Contains(character.CharacterName))
            .ToListAsync(cancellationToken);

        var charactersByName = projectCharacters.ToDictionary(
            character => character.CharacterName,
            StringComparer.Ordinal);

        var missingCharacter = stepData.CharacterPrompts
            .FirstOrDefault(prompt => !charactersByName.ContainsKey(prompt.Name));

        if (missingCharacter is not null)
        {
            throw new InvalidOperationException(
                $"The Portraits step references an unknown character: {missingCharacter.Name}.");
        }

        foreach (var characterPrompt in stepData.CharacterPrompts)
        {
            var character = charactersByName[characterPrompt.Name];

            if (!string.IsNullOrWhiteSpace(character.CharacterIllustrationPath))
            {
                continue;
            }

            var imageInteraction = await geminiClient.CreateImageInteractionAsync(
                $"Create an illustration for {characterPrompt.Name} following this description: {characterPrompt.Prompt}",
                stepData.ImageInteractionId,
                cancellationToken);

            var fileExtension = GetImageFileExtension(imageInteraction.MimeType);
            var illustrationsDirectory = Path.GetFullPath(
                fileStorageOptions.Value.IllustrationsDirectory);
            var filePath = Path.Combine(
                illustrationsDirectory,
                $"character{character.CharacterId}_book{portraitStep.ProjectId}{fileExtension}");

            Directory.CreateDirectory(illustrationsDirectory);
            await File.WriteAllBytesAsync(
                filePath,
                Convert.FromBase64String(imageInteraction.ImageData),
                cancellationToken);

            character.CharacterIllustrationPath = filePath;
            stepData.ImageInteractionId = imageInteraction.InteractionId;
            portraitStep.StepData = JsonSerializer.Serialize(stepData, StepDataJsonOptions);
            portraitStep.UpdatedAt = DateTime.UtcNow;

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
