using BookIllustration_Backend.Data;
using BookIllustration_Backend.Services.GeminiFeatures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookIllustration_Backend.Tests;

public class BookIllustrationApiFactory : WebApplicationFactory<global::Program>
{
    public FakeGeminiHandler GeminiHandler { get; } = new();

    public string DatabasePath { get; } = Path.Combine(
        Path.GetTempPath(),
        $"book-illustration-test-{Guid.NewGuid()}.db");

    public string IllustrationsDirectory { get; } = Path.Combine(
        Path.GetTempPath(),
        $"book-illustration-illustrations-{Guid.NewGuid()}");

    public string BookTextPath => Path.ChangeExtension(DatabasePath, ".txt");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDatabase"] = $"Data Source={DatabasePath}",
                    ["FileStorage:IllustrationsDirectory"] = IllustrationsDirectory,
                    ["JWT_SIGNING_KEY"] =
                        "test-signing-key-that-is-long-enough-for-jwt-validation",
                    ["Jwt:Issuer"] = "BookIllustrationTestBackend",
                    ["Jwt:Audience"] = "BookIllustrationTestFrontend"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={DatabasePath};Pooling=False"));

            services.AddHttpClient<GeminiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => GeminiHandler);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();

        if (disposing && File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }

        if (disposing && File.Exists(BookTextPath))
        {
            File.Delete(BookTextPath);
        }

        if (disposing && Directory.Exists(IllustrationsDirectory))
        {
            var temporaryDirectory = Path.GetFullPath(Path.GetTempPath());
            var illustrationsDirectory = Path.GetFullPath(IllustrationsDirectory);

            if (!illustrationsDirectory.StartsWith(
                    temporaryDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The test illustrations directory is not inside the temporary directory.");
            }

            Directory.Delete(illustrationsDirectory, recursive: true);
        }
    }
}
