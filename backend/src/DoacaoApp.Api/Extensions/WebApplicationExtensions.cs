using AspNetCoreRateLimit;
using DoacaoApp.Api.Endpoints;
using DoacaoApp.Api.Middleware;
using DoacaoApp.Infrastructure.Persistence;
using DoacaoApp.Infrastructure.Persistence.Seed;
using DoacaoApp.Infrastructure.Storage;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

namespace DoacaoApp.Api.Extensions;

/// <summary>Monta o pipeline HTTP e executa migrations/seed no startup.</summary>
public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();

        app.UseIpRateLimiting();

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapAuthEndpoints();
        app.MapPublicEndpoints();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        return app;
    }

    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

        if (configuration.GetValue<bool>("RunMigrationsOnStartup"))
        {
            logger.LogInformation("Aplicando migrations no startup...");
            var dbContext = services.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await DataSeeder.SeedAsync(services);
    }

    /// <summary>Garante que o bucket de storage exista (MinIO/S3) antes de receber uploads.</summary>
    public static async Task InitializeStorageAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<StorageBucketInitializer>();
        await initializer.EnsureBucketAsync();
    }
}