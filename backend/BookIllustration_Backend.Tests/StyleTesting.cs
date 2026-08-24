using System.Net;
using System.Net.Http.Json;
using BookIllustration_Backend.Data;
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

        Assert.Equal(HttpStatusCode.NoContent, styleResponse.StatusCode);

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

        var firstRequest = client.PostAsJsonAsync(
            styleUrl,
            new { style = (string?)null });

        await factory.GeminiHandler.WaitUntilPausedInteractionStartsAsync();

        var duplicateResponse = await client.PostAsJsonAsync(
            styleUrl,
            new { style = (string?)null });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        factory.GeminiHandler.ReleasePausedInteraction();

        var firstResponse = await firstRequest;

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
    }
}
