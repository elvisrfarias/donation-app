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
        app.UseForwardedHeaders();
        app.UseSerilogRequestLogging();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();

        app.UseIpRateLimiting();

        MapApiDocumentationOutsideProduction(app);

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

    private static void MapApiDocumentationOutsideProduction(WebApplication app)
    {
        if (app.Environment.IsProduction())
        {
            return;
        }

        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    /// <summary>
    /// Recusa subir em produção com um segredo que ainda é o valor placeholder versionado no
    /// `.env.example` — sem isso, o fluxo documentado (`cp .env.example .env`) permite ir pra
    /// produção com Jwt/admin/MinIO conhecidos publicamente por qualquer clone do repositório.
    /// </summary>
    public static void EnsureNoDefaultSecretsInProduction(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            return;
        }

        var configuration = app.Configuration;
        var knownPlaceholders = new (string Key, string Placeholder)[]
        {
            ("Jwt:Secret", "CHAVE-SUPER-SECRETA-COM-NO-MINIMO-32-CARACTERES-troque"),
            ("Jwt:Secret", "CHAVE-DE-DESENVOLVIMENTO-COM-NO-MINIMO-32-CARACTERES"),
            ("SeedData:AdminPassword", "Admin@123456"),
            ("Storage:SecretKey", "troque-esta-senha-minio"),
        };

        var offendingKeys = knownPlaceholders
            .Where(p => string.Equals(configuration[p.Key], p.Placeholder, StringComparison.Ordinal))
            .Select(p => p.Key)
            .Distinct()
            .ToList();

        if ((configuration.GetConnectionString("Database") ?? string.Empty)
            .Contains("troque-esta-senha-em-producao", StringComparison.Ordinal))
        {
            offendingKeys.Add("ConnectionStrings:Database");
        }

        if (offendingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"ASPNETCORE_ENVIRONMENT=Production com valor padrão do .env.example em: " +
                $"{string.Join(", ", offendingKeys)}. Troque esses segredos antes de subir em produção.");
        }
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
