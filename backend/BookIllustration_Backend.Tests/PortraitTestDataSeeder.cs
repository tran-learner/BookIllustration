using System.Text.Json;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public record SeededPortraitProject(int ProjectId, string Email);

public static class PortraitTestDataSeeder
{
    public static async Task<SeededPortraitProject> SeedAsync(
        BookIllustrationApiFactory factory)
    {
        const string email = "portrait-test@example.com";

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = email,
            FullName = "Portrait Test User"
        };

        var project = new Project
        {
            ProjectTitle = "Portrait Test Project",
            BookTextPath = factory.BookTextPath,
            CreatedAt = DateTime.UtcNow,
            Style = "Whimsical watercolor storybook illustration.",
            UserEmail = user.Email,
            User = user
        };

        var alicePrompt = new CharacterPrompt
        {
            Name = "Alice",
            Prompt = "Alice is an adult woman with warm brown eyes, a gentle smile, a blue wool coat, and a weathered leather satchel. Create a detailed storybook portrait with soft watercolor texture, natural morning light, delicate facial features, and a calm, curious expression."
        };
        var bobPrompt = new CharacterPrompt
        {
            Name = "Bob",
            Prompt = "Bob is an adult man with a thoughtful expression, silver hair, a moss-green waistcoat, and round spectacles. Create a detailed storybook portrait with watercolor texture, gentle afternoon light, expressive facial features, and a quietly adventurous personality."
        };

        var alice = new Character
        {
            CharacterName = alicePrompt.Name,
            CharacterDescription = alicePrompt.Prompt,
            Project = project
        };
        var bob = new Character
        {
            CharacterName = bobPrompt.Name,
            CharacterDescription = bobPrompt.Prompt,
            Project = project
        };

        var portraitStep = new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Portraits,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            StepData = JsonSerializer.Serialize(
                new PortraitStepData
                {
                    CharacterInteractionId = "character-interaction-id",
                    CharacterPrompts = [alicePrompt, bobPrompt]
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            UpdatedAt = DateTime.UtcNow,
            Project = project
        };

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.Characters.AddRange(alice, bob);
        dbContext.PipelineSteps.Add(portraitStep);

        await dbContext.SaveChangesAsync();

        return new SeededPortraitProject(project.ProjectId, user.Email);
    }
}
