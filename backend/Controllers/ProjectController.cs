using System.Security.Claims;
using BookIllustration_Backend.Models.DTOs.Projects;
using BookIllustration_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookIllustration_Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectController(
    ProjectService projectService,
    ILogger<ProjectController> logger) : ControllerBase
{
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
}
