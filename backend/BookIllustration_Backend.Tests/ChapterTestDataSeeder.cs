using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public record SeededChapterProject(int ProjectId, string Email);

public static class ChapterTestDataSeeder
{
    public static async Task<SeededChapterProject> SeedAsync(
        BookIllustrationApiFactory factory)
    {
        const string email = "chapter-test@example.com";

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = email,
            FullName = "Chapter Test User"
        };

        var project = new Project
        {
            ProjectTitle = "Chapter Test Project",
            BookTextPath = factory.BookTextPath,
            CreatedAt = DateTime.UtcNow,
            Style = "Whimsical watercolor storybook illustration.",
            UserEmail = user.Email,
            User = user
        };

        var chapterStep = new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Chapters,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new ChapterStepData
                {
                    CharacterInteractionId = "chapter-character-interaction-id",
                    ImageInteractionId = "chapter-image-interaction-id"
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            UpdatedAt = DateTime.UtcNow,
            Project = project
        };

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.PipelineSteps.Add(chapterStep);

        await dbContext.SaveChangesAsync();

        return new SeededChapterProject(project.ProjectId, user.Email);
    }
}
