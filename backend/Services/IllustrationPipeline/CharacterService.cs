using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.EntityFrameworkCore;

namespace BookIllustration_Backend.Services.IllustrationPipeline;

public class CharacterService(AppDbContext dbContext, GeminiClient geminiClient)
{
    private static readonly JsonSerializerOptions StepDataJsonOptions =
        new(JsonSerializerDefaults.Web);

    private const string CharacterPromptInstruction =
        "Can you describe the main characters (only the adults) and prepare a prompt describing them with as much details as possible (use the descriptions from the book) so Nano Banana can generate images of them? Each prompt should be at least 50 words.";

    private static readonly object CharacterResponseFormat = new
    {
        type = "text",
        mime_type = "application/json",
        schema = new
        {
            type = "array",
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

    public async Task RunCharacterStepAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        const int staleTimeoutMinutes = 5;

        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-staleTimeoutMinutes);

        var characterStep = await dbContext.PipelineSteps
            .AsNoTracking()
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Characters,
                cancellationToken);

        if (characterStep is null)
        {
            throw new InvalidOperationException("The Characters step was not found.");
        }

        if (characterStep.Status == PipelineStepStatus.Completed)
        {
            throw new InvalidOperationException("The Characters step has already completed.");
        }

        if (characterStep.Status == PipelineStepStatus.Running
            && characterStep.UpdatedAt > staleThreshold)
        {
            throw new InvalidOperationException("The Characters step is already running.");
        }

        var runId = Guid.NewGuid();

        var claimedRows = await dbContext.PipelineSteps
            .Where(step => step.PipelineStepId == characterStep.PipelineStepId
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
                "The Characters step state changed before this request could start.");
        }

        try
        {
            await SetCharactersAsync(projectId, runId, cancellationToken);
        }
        catch (Exception exception)
        {
            await dbContext.PipelineSteps
                .Where(step => step.PipelineStepId == characterStep.PipelineStepId
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

    public async Task SetCharactersAsync(
        int projectId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var characterStep = await dbContext.PipelineSteps
            .Include(step => step.Project)
            .SingleOrDefaultAsync(
                step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Characters
                    && step.Status != PipelineStepStatus.Completed
                    && step.RunId == runId,
                cancellationToken);

        if (characterStep is null)
        {
            throw new InvalidOperationException(
                "The Characters step was not found or has already completed.");
        }

        var stepData = string.IsNullOrWhiteSpace(characterStep.StepData)
            ? new CharacterStepData()
            : JsonSerializer.Deserialize<CharacterStepData>(
                characterStep.StepData,
                StepDataJsonOptions) ?? new CharacterStepData();

        if (string.IsNullOrWhiteSpace(stepData.StyleInteractionId))
        {
            throw new InvalidOperationException(
                "The Characters step is missing its Style interaction ID.");
        }

        if (!string.IsNullOrWhiteSpace(stepData.CharacterInteractionId))
        {
            throw new InvalidOperationException(
                "The Characters step has an interaction ID but did not complete.");
        }

        var characterInteraction = await geminiClient.CreateTextInteractionAsync(
            CharacterPromptInstruction,
            stepData.StyleInteractionId,
            cancellationToken,
            responseFormat: CharacterResponseFormat);

        if (string.IsNullOrWhiteSpace(characterInteraction.Text))
        {
            throw new InvalidOperationException("Gemini returned an empty character response.");
        }

        var characterPrompts = JsonSerializer.Deserialize<List<CharacterPrompt>>(
            characterInteraction.Text,
            StepDataJsonOptions);

        if (characterPrompts is null || characterPrompts.Count == 0
            || characterPrompts.Any(character =>
                string.IsNullOrWhiteSpace(character.Name)
                || string.IsNullOrWhiteSpace(character.Prompt)))
        {
            throw new InvalidOperationException(
                "Gemini returned an invalid character response.");
        }

        stepData.CharacterInteractionId = characterInteraction.InteractionId;
        characterStep.StepData = JsonSerializer.Serialize(stepData, StepDataJsonOptions);

        foreach (var characterPrompt in characterPrompts)
        {
            dbContext.Characters.Add(new Character
            {
                CharacterName = characterPrompt.Name,
                CharacterDescription = characterPrompt.Prompt,
                ProjectId = characterStep.ProjectId,
                Project = characterStep.Project
            });
        }

        characterStep.Status = PipelineStepStatus.Completed;
        characterStep.CompletedAt = DateTime.UtcNow;
        characterStep.UpdatedAt = DateTime.UtcNow;

        dbContext.PipelineSteps.Add(new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Portraits,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new PortraitStepData
                {
                    CharacterInteractionId = characterInteraction.InteractionId,
                    CharacterPrompts = characterPrompts
                },
                StepDataJsonOptions),
            UpdatedAt = characterStep.UpdatedAt.AddTicks(1),
            ProjectId = characterStep.ProjectId,
            Project = characterStep.Project
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
