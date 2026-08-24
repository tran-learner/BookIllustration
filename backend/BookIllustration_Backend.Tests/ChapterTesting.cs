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

public class ChapterTesting
{
    [Fact]
    public async Task RunChapters_WithOwnedPendingStep_CreatesChapterAndIllustrationsStep()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await ChapterTestDataSeeder.SeedAsync(factory);

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
                fullName = "Chapter Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var chapterResponse = await client.PostAsync(
            $"/api/projects/{seededProject.ProjectId}/pipeline/chapters",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, chapterResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var chapter = await dbContext.Chapters.SingleAsync(
            chapter => chapter.ProjectId == seededProject.ProjectId);
        var chapterStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Chapters);
        var illustrationsStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Illustrations);

        var chapterStepData = JsonSerializer.Deserialize<ChapterStepData>(
            chapterStep.StepData!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var illustrationStepData = JsonSerializer.Deserialize<IllustrationStepData>(
            illustrationsStep.StepData!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(PipelineStepStatus.Completed, chapterStep.Status);
        Assert.Equal(PipelineStepStatus.Pending, illustrationsStep.Status);
        Assert.Equal("Chapter One", chapter.ChapterTitle);
        Assert.Contains("single-image storybook illustration", chapter.ChapterDescription);
        Assert.Equal("chapter-interaction-id", chapterStepData?.ChapterInteractionId);

        var chapterPrompt = Assert.Single(chapterStepData!.ChapterPrompts);
        Assert.Equal("Chapter One", chapterPrompt.Name);
        Assert.Contains("single-image storybook illustration", chapterPrompt.Prompt);
        Assert.Equal(
            "chapter-image-interaction-id",
            illustrationStepData?.ImageInteractionId);
    }

    [Fact]
    public async Task RunChapters_WhileAnotherRequestIsRunning_ReturnsConflict()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await ChapterTestDataSeeder.SeedAsync(factory);
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
                fullName = "Chapter Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var chapterUrl =
            $"/api/projects/{seededProject.ProjectId}/pipeline/chapters";

        var firstRequest = client.PostAsync(chapterUrl, content: null);

        await factory.GeminiHandler.WaitUntilPausedInteractionStartsAsync();

        var duplicateResponse = await client.PostAsync(chapterUrl, content: null);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        factory.GeminiHandler.ReleasePausedInteraction();

        var firstResponse = await firstRequest;

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
    }
}
