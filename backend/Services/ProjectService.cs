using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.Configuration;
using BookIllustration_Backend.Models.DTOs.Projects;
using BookIllustration_Backend.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookIllustration_Backend.Services;

public class ProjectService(
    AppDbContext dbContext,
    IOptions<FileStorageOptions> fileStorageOptions)
{
    public Task<string?> GetBookTextPathAsync(
        int projectId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();

        return dbContext.Projects
            .AsNoTracking()
            .Where(project => project.ProjectId == projectId
                && project.UserEmail == normalizedEmail)
            .Select(project => project.BookTextPath)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ProjectDetailResponse?> GetProjectByIdAsync(
        int projectId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();

        return dbContext.Projects
            .AsNoTracking()
            .Where(project => project.ProjectId == projectId
                && project.UserEmail == normalizedEmail)
            .Select(project => new ProjectDetailResponse
            {
                ProjectId = project.ProjectId,
                ProjectTitle = project.ProjectTitle,
                CreatedAt = project.CreatedAt,
                Style = project.Style,
                PipelineSteps = project.PipelineSteps
                    .OrderBy(step => step.UpdatedAt)
                    .Select(step => new PipelineStepResponse
                    {
                        PipelineStepId = step.PipelineStepId,
                        StepName = step.StepName,
                        Status = step.Status,
                        AttemptCount = step.AttemptCount,
                        StepData = step.StepData,
                        StartedAt = step.StartedAt,
                        UpdatedAt = step.UpdatedAt,
                        CompletedAt = step.CompletedAt,
                        ErrorMessage = step.ErrorMessage
                    })
                    .ToList(),
                Characters = project.Characters
                    .OrderBy(character => character.CharacterId)
                    .Select(character => new CharacterResponse
                    {
                        CharacterId = character.CharacterId,
                        CharacterName = character.CharacterName,
                        CharacterDescription = character.CharacterDescription
                    })
                    .ToList(),
                Chapters = project.Chapters
                    .OrderBy(chapter => chapter.ChapterId)
                    .Select(chapter => new ChapterResponse
                    {
                        ChapterId = chapter.ChapterId,
                        ChapterTitle = chapter.ChapterTitle,
                        ChapterDescription = chapter.ChapterDescription
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<ProjectListItemResponse>> GetProjectsByUserAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();

        return dbContext.Projects
            .AsNoTracking()
            .Where(project => project.UserEmail == normalizedEmail)
            .OrderByDescending(project => project.CreatedAt)
            .Select(project => new ProjectListItemResponse
            {
                ProjectId = project.ProjectId,
                ProjectTitle = project.ProjectTitle,
                CreatedAt = project.CreatedAt,
                Style = project.Style,
                CompletedStepCount = project.PipelineSteps.Count(
                    step => step.Status == PipelineStepStatus.Completed),
                LatestPipelineStep = project.PipelineSteps
                    .OrderByDescending(step => step.UpdatedAt)
                    .Select(step => new PipelineStepResponse
                    {
                        PipelineStepId = step.PipelineStepId,
                        StepName = step.StepName,
                        Status = step.Status,
                        AttemptCount = step.AttemptCount,
                        StepData = step.StepData,
                        StartedAt = step.StartedAt,
                        UpdatedAt = step.UpdatedAt,
                        CompletedAt = step.CompletedAt,
                        ErrorMessage = step.ErrorMessage
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateProjectAsync(
        string userEmail,
        string projectTitle,
        IFormFile bookFile,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        var normalizedTitle = projectTitle.Trim();

        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new ArgumentException("Project title is required.");
        }

        if (bookFile.Length == 0)
        {
            throw new ArgumentException("Book file must not be empty.");
        }

        if (!string.Equals(
                Path.GetExtension(bookFile.FileName),
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Book file must be a .txt file.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken)
            ?? throw new InvalidOperationException("The current user was not found.");

        var booksDirectory = Path.GetFullPath(
            fileStorageOptions.Value.BooksDirectory);
        var bookPath = Path.Combine(booksDirectory, $"{Guid.NewGuid():N}.txt");

        Directory.CreateDirectory(booksDirectory);

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var stream = new FileStream(
                bookPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await bookFile.CopyToAsync(stream, cancellationToken);
            }

            var now = DateTime.UtcNow;
            var project = new Project
            {
                ProjectTitle = normalizedTitle,
                BookTextPath = bookPath,
                CreatedAt = now,
                UserEmail = user.Email,
                User = user
            };

            var styleStep = new PipelineStep
            {
                PipelineStepId = Guid.NewGuid(),
                StepName = PipelineStepName.Style,
                Status = PipelineStepStatus.Pending,
                AttemptCount = 0,
                UpdatedAt = now,
                Project = project
            };

            dbContext.Projects.Add(project);
            dbContext.PipelineSteps.Add(styleStep);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return project.ProjectId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            if (File.Exists(bookPath))
            {
                File.Delete(bookPath);
            }

            throw;
        }
    }
}
