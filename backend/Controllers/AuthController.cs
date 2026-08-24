using System.Security.Claims;
using BookIllustration_Backend.Models.DTOs.Authentication;
using BookIllustration_Backend.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookIllustration_Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [Authorize]
    [HttpGet("session")]
    public ActionResult GetCurrentSession()
    {
        var fullName = User.FindFirst(ClaimTypes.Name)?.Value;

        return string.IsNullOrWhiteSpace(fullName)
            ? Unauthorized()
            : Ok(new { fullName });
    }

    [HttpPost("session")]
    public async Task<ActionResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await authService.CreateSessionAsync(
                request.Email,
                request.FullName,
                cancellationToken);

            Response.Cookies.Append(
                "access_token",
                session.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = session.ExpiresAt,
                    IsEssential = true,
                    Path = "/"
                });

            return Ok(new { session.ExpiresAt });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("session")]
    public IActionResult DeleteSession()
    {
        Response.Cookies.Delete(
            "access_token",
            new CookieOptions
            {
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

        return NoContent();
    }
}
