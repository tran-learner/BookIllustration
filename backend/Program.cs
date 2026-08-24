using System.Text;
using BookIllustration_Backend.Data;
using BookIllustration_Backend.Services.Authentication;
using BookIllustration_Backend.Services.GeminiFeatures;
using BookIllustration_Backend.Services.IllustrationPipeline;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDatabase")
    ?? throw new InvalidOperationException("Connection string 'AppDatabase' was not found.");

var geminiOptions = builder.Configuration
    .GetSection(GeminiOptions.SectionName)
    .Get<GeminiOptions>()
    ?? throw new InvalidOperationException("Gemini configuration was not found.");

geminiOptions.ApiKey = builder.Configuration["GEMINI_API_KEY"]
    ?? throw new InvalidOperationException("GEMINI_API_KEY was not found.");

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration was not found.");

jwtOptions.SigningKey = builder.Configuration["JWT_SIGNING_KEY"]
    ?? throw new InvalidOperationException("JWT_SIGNING_KEY was not found.");

if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "JWT_SIGNING_KEY must be at least 32 bytes long.");
}

builder.Services.AddSingleton(geminiOptions);
builder.Services.AddSingleton(jwtOptions);

builder.Services.AddHttpClient<GeminiClient>(client =>
{
    client.BaseAddress = new Uri(geminiOptions.BaseUrl);
    client.DefaultRequestHeaders.Add("x-goog-api-key", geminiOptions.ApiKey);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StyleService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
