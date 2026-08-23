using BookIllustration_Backend.Data;
using BookIllustration_Backend.Services.GeminiFeatures;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddSingleton(geminiOptions);

builder.Services.AddHttpClient<GeminiClient>(client =>
{
    client.BaseAddress = new Uri(geminiOptions.BaseUrl);
    client.DefaultRequestHeaders.Add("x-goog-api-key", geminiOptions.ApiKey);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

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

app.UseAuthorization();

app.MapControllers();

app.Run();
