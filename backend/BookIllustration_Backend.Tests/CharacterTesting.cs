using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public class CharacterTesting
{
    [Fact]
    public async Task RunCharacters_WithOwnedPendingProject_CompletesAndCreatesPortraitsStep()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await CharacterTestDataSeeder.SeedAsync(factory);

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

        var signInResponse = await client.PostAsJsonAsync(
            "/api/auth/session",
            new
            {
                email = seededProject.Email,
                fullName = "Character Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var characterResponse = await client.PostAsync(
            $"/api/projects/{seededProject.ProjectId}/pipeline/characters",
            content: null);

        Assert.Equal(HttpStatusCode.Accepted, characterResponse.StatusCode);

        await WaitForCharacterStepStatusAsync(
            factory,
            seededProject.ProjectId,
            PipelineStepStatus.Completed);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var characterStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Characters);
        var portraitsStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Portraits);

        Assert.Equal(PipelineStepStatus.Completed, characterStep.Status);
        Assert.Equal(PipelineStepStatus.Pending, portraitsStep.Status);
        Assert.NotNull(portraitsStep.StepData);

        var portraitStepData = JsonSerializer.Deserialize<PortraitStepData>(
            portraitsStep.StepData!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(portraitStepData);
        Assert.Equal(
            "character-interaction-id",
            portraitStepData!.CharacterInteractionId);

        var characterPrompt = Assert.Single(portraitStepData.CharacterPrompts);
        Assert.Equal("Alice", characterPrompt.Name);
        Assert.Contains(
            "Alice is an adult woman with warm brown eyes",
            characterPrompt.Prompt);
    }

    [Fact]
    public async Task RunCharacters_WhileAnotherRequestIsRunning_ReturnsConflict()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await CharacterTestDataSeeder.SeedAsync(factory);
        factory.GeminiHandler.PauseNextInteraction();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

        var signInResponse = await client.PostAsJsonAsync(
            "/api/auth/session",
            new
            {
                email = seededProject.Email,
                fullName = "Character Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var characterUrl =
            $"/api/projects/{seededProject.ProjectId}/pipeline/characters";

        var firstResponse = await client.PostAsync(characterUrl, content: null);

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);

        await factory.GeminiHandler.WaitUntilPausedInteractionStartsAsync();

        var duplicateResponse = await client.PostAsync(characterUrl, content: null);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        factory.GeminiHandler.ReleasePausedInteraction();

        await WaitForCharacterStepStatusAsync(
            factory,
            seededProject.ProjectId,
            PipelineStepStatus.Completed);
    }

    private static async Task WaitForCharacterStepStatusAsync(
        BookIllustrationApiFactory factory,
        int projectId,
        PipelineStepStatus expectedStatus)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < timeoutAt)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var status = await dbContext.PipelineSteps
                .Where(step => step.ProjectId == projectId
                    && step.StepName == PipelineStepName.Characters)
                .Select(step => step.Status)
                .SingleAsync();

            if (status == expectedStatus)
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException(
            $"The Characters step did not reach {expectedStatus} within five seconds.");
    }
}
