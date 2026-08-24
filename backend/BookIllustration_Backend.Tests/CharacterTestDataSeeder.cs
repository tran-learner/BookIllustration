using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public record SeededCharacterProject(int ProjectId, string Email);

public static class CharacterTestDataSeeder
{
    public static async Task<SeededCharacterProject> SeedAsync(
        BookIllustrationApiFactory factory)
    {
        const string email = "character-test@example.com";
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = email,
            FullName = "Character Test User"
        };

        var project = new Project
        {
            ProjectTitle = "Character Test Project",
            BookTextPath = factory.BookTextPath,
            CreatedAt = DateTime.UtcNow,
            UserEmail = user.Email,
            User = user
        };

        var characterStep = new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Characters,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new CharacterStepData
                {
                    StyleInteractionId = "style-interaction-id"
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            UpdatedAt = DateTime.UtcNow,
            Project = project
        };

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.PipelineSteps.Add(characterStep);
        await dbContext.SaveChangesAsync();

        return new SeededCharacterProject(project.ProjectId, user.Email);
    }
}
