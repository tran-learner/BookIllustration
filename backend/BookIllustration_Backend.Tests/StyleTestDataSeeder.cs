using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookIllustration_Backend.Tests;

public record SeededStyleProject(int ProjectId, string Email);

public static class StyleTestDataSeeder
{
    public static async Task<SeededStyleProject> SeedAsync(
        BookIllustrationApiFactory factory)
    {
        const string email = "style-test@example.com";

        await File.WriteAllTextAsync(
            factory.BookTextPath,
            "A short test book about a curious fox.");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = email,
            FullName = "Style Test User"
        };

        var project = new Project
        {
            ProjectTitle = "Style Test Project",
            BookTextPath = factory.BookTextPath,
            CreatedAt = DateTime.UtcNow,
            UserEmail = user.Email,
            User = user
        };

        var styleStep = new PipelineStep
        {
            PipelineStepId = Guid.NewGuid(),
            StepName = PipelineStepName.Style,
            Status = PipelineStepStatus.Pending,
            AttemptCount = 0,
            UpdatedAt = DateTime.UtcNow,
            Project = project
        };

        dbContext.Users.Add(user);
        dbContext.Projects.Add(project);
        dbContext.PipelineSteps.Add(styleStep);

        await dbContext.SaveChangesAsync();

        return new SeededStyleProject(project.ProjectId, user.Email);
    }
}
