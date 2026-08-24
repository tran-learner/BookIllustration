using System.Security.Claims;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Pipeline;
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
    StyleService styleService,
    CharacterService characterService,
    PortraitService portraitService) : ControllerBase
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
            await styleService.RunStyleStepAsync(
                projectId,
                request.Style,
                cancellationToken);

            return NoContent();
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
            await characterService.RunCharacterStepAsync(projectId, cancellationToken);
            return NoContent();
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
            await portraitService.RunPortraitStepAsync(projectId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}
