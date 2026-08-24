using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Models.DTOs.Authentication;
using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BookIllustration_Backend.Services.Authentication;

public class AuthService(AppDbContext dbContext, JwtOptions jwtOptions)
{
    public async Task<AuthSession> CreateSessionAsync(
        string email,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedFullName = fullName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedEmail)
            || string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new ArgumentException("Email and full name are required.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = normalizedEmail,
                FullName = normalizedFullName
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.ExpirationMinutes);
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName)
            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        return new AuthSession
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }
}
