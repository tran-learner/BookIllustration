using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.AspNetCore.Mvc;

namespace BookIllustration_Backend.Controllers;

[ApiController]
[Route("api/gemini-test")]
public class GeminiTestController(GeminiClient geminiClient) : ControllerBase
{
    [HttpPost("text")]
    public async Task<ActionResult> CreateTextInteraction(
        [FromBody] string input,
        CancellationToken cancellationToken)
    {
        var interaction = await geminiClient.CreateTextInteractionAsync(
            input,
            cancellationToken: cancellationToken);

        return Ok(interaction);
    }
}
