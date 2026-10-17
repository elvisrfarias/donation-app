using System.Data.Common;
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

    private const string GeneratePlaceholder = "GERAR";

    private static readonly (string Key, string Placeholder)[] KnownSecretPlaceholders =
    [
        ("Jwt:Secret", "CHAVE-SUPER-SECRETA-COM-NO-MINIMO-32-CARACTERES-troque"),
        ("Jwt:Secret", "CHAVE-DE-DESENVOLVIMENTO-COM-NO-MINIMO-32-CARACTERES"),
        ("Jwt:Secret", GeneratePlaceholder),
        ("SeedData:AdminPassword", "Admin@123456"),
        ("Storage:SecretKey", "troque-esta-senha-minio"),
        ("Storage:SecretKey", GeneratePlaceholder),
    ];

    private static readonly string[] KnownDatabasePasswordPlaceholders = ["troque-esta-senha-em-producao", GeneratePlaceholder];

    /// <summary>
    /// Recusa subir em produção com um segredo que ainda é o valor placeholder versionado no
    /// `.env.example`/`.env.prod.example` — sem isso, o fluxo documentado (`cp .env.example .env`) permite ir
    /// pra produção com Jwt/admin/MinIO/Postgres conhecidos publicamente por qualquer clone do repositório.
    /// A senha do admin de seed só é conferida contra "GERAR" quando o seed está habilitado (em produção ela
    /// costuma ficar vazia com o seed desligado).
    /// </summary>
    public static void EnsureNoDefaultSecretsInProduction(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            return;
        }

        var offendingKeys = FindDefaultSecrets(app.Configuration);
        if (offendingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"ASPNETCORE_ENVIRONMENT=Production com valor padrão do .env.example em: " +
                $"{string.Join(", ", offendingKeys)}. Troque esses segredos antes de subir em produção.");
        }
    }

    /// <summary>Chaves de configuração que ainda trazem um placeholder versionado (exposto para testes).</summary>
    public static IReadOnlyList<string> FindDefaultSecrets(IConfiguration configuration)
    {
        var offendingKeys = KnownSecretPlaceholders
            .Where(p => IsPlaceholder(configuration[p.Key], p.Placeholder))
            .Select(p => p.Key)
            .ToList();

        if (configuration.GetValue<bool>("SeedData:Enabled")
            && IsPlaceholder(configuration["SeedData:AdminPassword"], GeneratePlaceholder))
        {
            offendingKeys.Add("SeedData:AdminPassword");
        }

        if (HasPlaceholderDatabasePassword(configuration.GetConnectionString("Database")))
        {
            offendingKeys.Add("ConnectionStrings:Database");
        }

        return offendingKeys.Distinct().ToList();
    }

    private static bool IsPlaceholder(string? value, string placeholder) =>
        string.Equals(value?.Trim(), placeholder, StringComparison.Ordinal);

    private static bool HasPlaceholderDatabasePassword(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var builder = new DbConnectionStringBuilder();
        try
        {
            builder.ConnectionString = connectionString;
        }
        catch (ArgumentException)
        {
            return KnownDatabasePasswordPlaceholders.Any(p => connectionString.Contains(p, StringComparison.Ordinal));
        }

        return (builder.TryGetValue("Password", out var password) || builder.TryGetValue("Pwd", out password))
            && KnownDatabasePasswordPlaceholders.Any(p => IsPlaceholder(password?.ToString(), p));
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
