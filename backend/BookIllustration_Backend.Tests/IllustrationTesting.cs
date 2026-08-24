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

public class IllustrationTesting
{
    [Fact]
    public async Task RunIllustrations_WithOwnedPendingStep_GeneratesMissingIllustrations()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await IllustrationTestDataSeeder.SeedAsync(factory);

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
                fullName = "Illustration Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var illustrationResponse = await client.PostAsync(
            $"/api/projects/{seededProject.ProjectId}/pipeline/illustrations",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, illustrationResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var chapter = await dbContext.Chapters.SingleAsync(
            chapter => chapter.ProjectId == seededProject.ProjectId);
        var illustrationsStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Illustrations);
        var stepData = JsonSerializer.Deserialize<IllustrationStepData>(
            illustrationsStep.StepData!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(PipelineStepStatus.Completed, illustrationsStep.Status);
        Assert.False(string.IsNullOrWhiteSpace(chapter.ChapterIllustrationPath));
        Assert.True(File.Exists(chapter.ChapterIllustrationPath));
        Assert.False(string.IsNullOrWhiteSpace(stepData?.ChapterImageInteractionId));
        Assert.Equal(1, Directory.GetFiles(factory.IllustrationsDirectory).Length);
    }

    [Fact]
    public async Task RunIllustrations_WhileAnotherRequestIsRunning_ReturnsConflict()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await IllustrationTestDataSeeder.SeedAsync(factory);
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
                fullName = "Illustration Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var illustrationUrl =
            $"/api/projects/{seededProject.ProjectId}/pipeline/illustrations";

        var firstRequest = client.PostAsync(illustrationUrl, content: null);

        await factory.GeminiHandler.WaitUntilPausedInteractionStartsAsync();

        var duplicateResponse = await client.PostAsync(illustrationUrl, content: null);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        factory.GeminiHandler.ReleasePausedInteraction();

        var firstResponse = await firstRequest;

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
    }
}
