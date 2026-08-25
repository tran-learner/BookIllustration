using System.Security.Claims;
using BookIllustration_Backend.Models.Configuration;
using BookIllustration_Backend.Models.DTOs.Projects;
using BookIllustration_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookIllustration_Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectController(
    ProjectService projectService,
    IOptions<FileStorageOptions> fileStorageOptions,
    ILogger<ProjectController> logger) : ControllerBase
{
    [HttpGet("{projectId:int}/book-text")]
    public async Task<IActionResult> GetBookText(
        int projectId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var bookTextPath = await projectService.GetBookTextPathAsync(
            projectId,
            userEmail,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(bookTextPath))
        {
            return NotFound();
        }

        var booksDirectory = Path.GetFullPath(
            fileStorageOptions.Value.BooksDirectory);
        var fullBookPath = Path.GetFullPath(bookTextPath);

        if (!fullBookPath.StartsWith(
                booksDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(fullBookPath))
        {
            return NotFound();
        }

        var bookFileInfo = new FileInfo(fullBookPath);

        if (bookFileInfo.Length > fileStorageOptions.Value.MaxBookTextPreviewBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new { message = "The book file is too large to display." });
        }

        return PhysicalFile(
            fullBookPath,
            "text/plain; charset=utf-8",
            enableRangeProcessing: true);
    }

    [HttpGet("{projectId:int}")]
    public async Task<ActionResult<ProjectDetailResponse>> GetProjectById(
        int projectId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var project = await projectService.GetProjectByIdAsync(
            projectId,
            userEmail,
            cancellationToken);

        return project is null ? NotFound() : Ok(project);
    }

    [HttpGet("{projectId:int}/characters/{characterId:int}/portrait")]
    public async Task<IActionResult> GetCharacterPortrait(
        int projectId,
        int characterId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var illustrationPath = await projectService.GetCharacterIllustrationPathAsync(
            projectId,
            characterId,
            userEmail,
            cancellationToken);

        return GetIllustrationFile(illustrationPath);
    }

    [HttpGet("{projectId:int}/chapters/{chapterId:int}/illustration")]
    public async Task<IActionResult> GetChapterIllustration(
        int projectId,
        int chapterId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var illustrationPath = await projectService.GetChapterIllustrationPathAsync(
            projectId,
            chapterId,
            userEmail,
            cancellationToken);

        return GetIllustrationFile(illustrationPath);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectListItemResponse>>> GetProjects(
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var projects = await projectService.GetProjectsByUserAsync(
            userEmail,
            cancellationToken);

        return Ok(projects);
    }

    [HttpPost]
    public async Task<ActionResult> CreateProject(
        [FromForm] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        try
        {
            var projectId = await projectService.CreateProjectAsync(
                userEmail,
                request.ProjectTitle,
                request.BookFile,
                cancellationToken);

            return Created($"/api/projects/{projectId}", new { projectId });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to create a project for {UserEmail}.",
                userEmail);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Unable to create the project. Please try again." });
        }
    }

    private IActionResult GetIllustrationFile(string? illustrationPath)
    {
        if (string.IsNullOrWhiteSpace(illustrationPath))
        {
            return NotFound();
        }

        var illustrationsDirectory = Path.GetFullPath(
            fileStorageOptions.Value.IllustrationsDirectory);
        var fullIllustrationPath = Path.GetFullPath(illustrationPath);

        if (!fullIllustrationPath.StartsWith(
                illustrationsDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            || !System.IO.File.Exists(fullIllustrationPath))
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(fullIllustrationPath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return PhysicalFile(
            fullIllustrationPath,
            contentType,
            enableRangeProcessing: true);
    }
}
