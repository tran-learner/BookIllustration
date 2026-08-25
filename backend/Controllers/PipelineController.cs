using System.Security.Claims;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
using BookIllustration_Backend.Models.Entities;
using BookIllustration_Backend.Services.IllustrationPipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookIllustration_Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/pipeline")]
public class PipelineController(
    AppDbContext dbContext,
    IPipelineJobQueue pipelineJobQueue,
    StyleService styleService,
    CharacterService characterService,
    PortraitService portraitService,
    ChapterService chapterService,
    ChapterIllustrationService chapterIllustrationService) : ControllerBase
{
    [HttpPost("style")]
    public async Task<IActionResult> RunStyle(
        int projectId,
        [FromBody] RunStyleRequest request,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var ownsProject = await dbContext.Projects.AnyAsync(
            project => project.ProjectId == projectId
                && project.UserEmail == userEmail,
            cancellationToken);

        if (!ownsProject)
        {
            return NotFound();
        }

        try
        {
            var runId = await styleService.ClaimStyleStepAsync(
                projectId,
                request.Style,
                cancellationToken);

            await pipelineJobQueue.EnqueueAsync(
                new PipelineJob(projectId, PipelineStepName.Style, runId),
                CancellationToken.None);

            return Accepted();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("characters")]
    public async Task<IActionResult> RunCharacters(
        int projectId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var ownsProject = await dbContext.Projects.AnyAsync(
            project => project.ProjectId == projectId
                && project.UserEmail == userEmail,
            cancellationToken);

        if (!ownsProject)
        {
            return NotFound();
        }

        try
        {
            var runId = await characterService.ClaimCharacterStepAsync(
                projectId,
                cancellationToken);

            await pipelineJobQueue.EnqueueAsync(
                new PipelineJob(projectId, PipelineStepName.Characters, runId),
                CancellationToken.None);

            return Accepted();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("portraits")]
    public async Task<IActionResult> RunPortraits(
        int projectId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var ownsProject = await dbContext.Projects.AnyAsync(
            project => project.ProjectId == projectId
                && project.UserEmail == userEmail,
            cancellationToken);

        if (!ownsProject)
        {
            return NotFound();
        }

        try
        {
            var runId = await portraitService.ClaimPortraitStepAsync(
                projectId,
                cancellationToken);

            await pipelineJobQueue.EnqueueAsync(
                new PipelineJob(projectId, PipelineStepName.Portraits, runId),
                CancellationToken.None);

            return Accepted();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("chapters")]
    public async Task<IActionResult> RunChapters(
        int projectId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var ownsProject = await dbContext.Projects.AnyAsync(
            project => project.ProjectId == projectId
                && project.UserEmail == userEmail,
            cancellationToken);

        if (!ownsProject)
        {
            return NotFound();
        }

        try
        {
            var runId = await chapterService.ClaimChapterStepAsync(
                projectId,
                cancellationToken);

            await pipelineJobQueue.EnqueueAsync(
                new PipelineJob(projectId, PipelineStepName.Chapters, runId),
                CancellationToken.None);

            return Accepted();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("illustrations")]
    public async Task<IActionResult> RunIllustrations(
        int projectId,
        CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Unauthorized();
        }

        var ownsProject = await dbContext.Projects.AnyAsync(
            project => project.ProjectId == projectId
                && project.UserEmail == userEmail,
            cancellationToken);

        if (!ownsProject)
        {
            return NotFound();
        }

        try
        {
            var runId = await chapterIllustrationService.ClaimIllustrationsStepAsync(
                projectId,
                cancellationToken);

            await pipelineJobQueue.EnqueueAsync(
                new PipelineJob(projectId, PipelineStepName.Illustrations, runId),
                CancellationToken.None);

            return Accepted();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}
