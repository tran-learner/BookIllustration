using System.Net;
using System.Net.Http.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public class PortraitTesting
{
    [Fact]
    public async Task RunPortraits_WithOwnedPendingStep_GeneratesMissingPortraits()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await PortraitTestDataSeeder.SeedAsync(factory);

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
                fullName = "Portrait Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var portraitResponse = await client.PostAsync(
            $"/api/projects/{seededProject.ProjectId}/pipeline/portraits",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, portraitResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var characters = await dbContext.Characters
            .Where(character => character.ProjectId == seededProject.ProjectId)
            .OrderBy(character => character.CharacterName)
            .ToListAsync();
        var portraitStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Portraits);
        var chapterStep = await dbContext.PipelineSteps.SingleAsync(
            step => step.ProjectId == seededProject.ProjectId
                && step.StepName == PipelineStepName.Chapters);

        Assert.Equal(2, characters.Count);
        Assert.All(characters, character =>
        {
            Assert.False(string.IsNullOrWhiteSpace(character.CharacterIllustrationPath));
            Assert.True(File.Exists(character.CharacterIllustrationPath));
        });
        Assert.Equal(PipelineStepStatus.Completed, portraitStep.Status);
        Assert.Equal(PipelineStepStatus.Pending, chapterStep.Status);
        Assert.Equal(2, Directory.GetFiles(factory.IllustrationsDirectory).Length);
    }

    [Fact]
    public async Task RunPortraits_WhileAnotherRequestIsRunning_ReturnsConflict()
    {
        using var factory = new BookIllustrationApiFactory();
        var seededProject = await PortraitTestDataSeeder.SeedAsync(factory);
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
                fullName = "Portrait Test User"
            });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var portraitUrl =
            $"/api/projects/{seededProject.ProjectId}/pipeline/portraits";

        var firstRequest = client.PostAsync(portraitUrl, content: null);

        await factory.GeminiHandler.WaitUntilPausedInteractionStartsAsync();

        var duplicateResponse = await client.PostAsync(portraitUrl, content: null);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        factory.GeminiHandler.ReleasePausedInteraction();

        var firstResponse = await firstRequest;

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
    }
}
