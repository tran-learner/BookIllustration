using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.Configuration;
using BookIllustration_Backend.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookIllustration_Backend.Services;

public class ProjectService(
    AppDbContext dbContext,
    IOptions<FileStorageOptions> fileStorageOptions)
{
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
