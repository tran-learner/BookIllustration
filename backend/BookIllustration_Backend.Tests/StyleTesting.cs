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

public class PipelineControllerTests
{
    [Fact]
    public async Task RunStyle_WithOwnedPendingProject_CompletesStyleAndCreatesCharactersStep()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await StyleTestDataSeeder.SeedAsync(factory);

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
                fullName = "Style Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var styleResponse = await client.PostAsJsonAsync(
            $"/api/projects/{seededProject.ProjectId}/pipeline/style",
            new { style = (string?)null });

        Assert.Equal(HttpStatusCode.Accepted, styleResponse.StatusCode);

        await WaitForStepStatusAsync(
            factory,
            seededProject.ProjectId,
            PipelineStepName.Style,
            PipelineStepStatus.Completed);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var project = await dbContext.Projects.SingleAsync(
            project => project.ProjectId == seededProject.ProjectId);

        var styleStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Style);

        var charactersStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Characters);

        Assert.Equal(
            "Whimsical watercolor storybook illustration.",
            project.Style);
        Assert.Equal(PipelineStepStatus.Completed, styleStep.Status);
        Assert.NotNull(styleStep.CompletedAt);
        Assert.Equal(PipelineStepStatus.Pending, charactersStep.Status);

        var characterStepData = JsonSerializer.Deserialize<CharacterStepData>(
            charactersStep.StepData!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal("style-interaction-id", characterStepData?.StyleInteractionId);
    }

    [Fact]
    public async Task RunStyle_WhileAnotherRequestIsRunning_ReturnsConflict()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await StyleTestDataSeeder.SeedAsync(factory);
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
                fullName = "Style Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var styleUrl =
            $"/api/projects/{seededProject.ProjectId}/pipeline/style";

        var firstResponse = await client.PostAsJsonAsync(
            styleUrl,
            new { style = (string?)null });

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);

        await factory.GeminiHandler.WaitUntilPausedInteractionStartsAsync();

        var duplicateResponse = await client.PostAsJsonAsync(
            styleUrl,
            new { style = (string?)null });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        factory.GeminiHandler.ReleasePausedInteraction();

        await WaitForStepStatusAsync(
            factory,
            seededProject.ProjectId,
            PipelineStepName.Style,
            PipelineStepStatus.Completed);

    }

    private static async Task WaitForStepStatusAsync(
        BookIllustrationApiFactory factory,
        int projectId,
        PipelineStepName stepName,
        PipelineStepStatus expectedStatus)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < timeoutAt)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var status = await dbContext.PipelineSteps
                .Where(step => step.ProjectId == projectId
                    && step.StepName == stepName)
                .Select(step => step.Status)
                .SingleAsync();

            if (status == expectedStatus)
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException(
            $"The {stepName} step did not reach {expectedStatus} within five seconds.");
    }
}
