using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using DoacaoApp.Api.Middleware;
using DoacaoApp.Api.OpenApi;
using DoacaoApp.Api.RateLimiting;
using DoacaoApp.Api.Services;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

namespace DoacaoApp.Api.Extensions;

/// <summary>Registra os serviços específicos da camada de API (web).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddJwtAuthentication(configuration);
        services.AddTrustedForwardedHeaders(configuration);
        services.AddRateLimiting(configuration);
        services.AddObservability(configuration);
        services.AddApiDocumentation();

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Seção 'Jwt' não configurada.");

        if (jwt.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret precisa ter pelo menos 32 caracteres (256 bits) para assinar com HS256.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name,
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = AccessTokenSessionValidator.ValidateAsync,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", p => p.RequireRole(Roles.Admin))
            .AddPolicy("ManagerOrAdmin", p => p.RequireRole(Roles.Admin, Roles.PointManager));
    }

    /// <summary>
    /// Só confia em X-Forwarded-For/-Proto quando o hop imediato vier da rede interna do compose
    /// (o nginx do frontend) — sem isso, um cliente batendo direto na API (porta publicada) podia
    /// forjar o header e contornar o rate limit por IP.
    /// </summary>
    private static void AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var cidr = configuration["ForwardedHeaders:KnownNetworkCidr"];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            if (string.IsNullOrWhiteSpace(cidr))
            {
                return;
            }

            var parts = cidr.Split('/');
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1])));
        });
    }

    private static void AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, IdPlaceholderRateLimitConfiguration>();
    }

    private static void AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database") ?? string.Empty;

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"]);
    }

    private static void AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
    }
}
