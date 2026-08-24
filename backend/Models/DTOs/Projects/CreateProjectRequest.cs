using Microsoft.AspNetCore.Http;

namespace BookIllustration_Backend.Models.DTOs.Projects;

public class CreateProjectRequest
{
    public required string ProjectTitle { get; set; }

    public required IFormFile BookFile { get; set; }
}
