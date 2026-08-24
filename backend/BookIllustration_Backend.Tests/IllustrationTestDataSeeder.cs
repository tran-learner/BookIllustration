using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public record SeededIllustrationProject(int ProjectId, string Email);

public static class IllustrationTestDataSeeder
{
    public static async Task<SeededIllustrationProject> SeedAsync(
        BookIllustrationApiFactory factory)
    {
        const string email = "illustration-test@example.com";
        const string chapterName = "Chapter One";
        const string chapterPrompt =
            "A detailed single-image storybook illustration of the opening chapter, with warm watercolor texture, an expressive riverside setting, and carefully described adult characters.";

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = email,
            FullName = "Illustration Test User"
        };

        var project = new Project
        {
            ProjectTitle = "Illustration Test Project",
            BookTextPath = factory.BookTextPath,
            CreatedAt = DateTime.UtcNow,
            Style = "Whimsical watercolor storybook illustration.",
            UserEmail = user.Email,
            User = user
        };

        var chapter = new Chapter
        {
            ChapterTitle = chapterName,
            ChapterDescription = chapterPrompt,
            Project = project
        };

        var illustrationsStep = new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Illustrations,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new IllustrationStepData
                {
                    ImageInteractionId = "portrait-final-image-interaction-id",
                    ChapterPrompts =
                    [
                        new ChapterPrompt
                        {
                            Name = chapterName,
                            Prompt = chapterPrompt
                        }
                    ]
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            UpdatedAt = DateTime.UtcNow,
            Project = project
        };

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.Chapters.Add(chapter);
        dbContext.PipelineSteps.Add(illustrationsStep);

        await dbContext.SaveChangesAsync();

        return new SeededIllustrationProject(project.ProjectId, user.Email);
    }
}
