using Amazon.Runtime;
using Amazon.S3;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.Reviews;
using DoacaoApp.Domain.Storage;
using DoacaoApp.Infrastructure.Authentication;
using DoacaoApp.Infrastructure.Identity;
using DoacaoApp.Infrastructure.Persistence;
using DoacaoApp.Infrastructure.Persistence.Interceptors;
using DoacaoApp.Infrastructure.Persistence.Repositories;
using DoacaoApp.Infrastructure.Persistence.Seed;
using DoacaoApp.Infrastructure.Services;
using DoacaoApp.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure;

/// <summary>Composition root da Infrastructure: EF Core, Identity (BCrypt), repositórios e serviços.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.AddPersistence(configuration);
        services.AddIdentityWithBCrypt();
        services.AddObjectStorage(configuration);

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICollectionPointRepository, CollectionPointRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IStoredFileRepository, StoredFileRepository>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<JwtTokenGenerator>();

        return services;
    }

    private static void AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
            var config = new AmazonS3Config { ForcePathStyle = options.ForcePathStyle };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl;
                config.AuthenticationRegion = options.Region;
            }
            else
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
            }

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddSingleton<IFileStorage, S3FileStorage>();
        services.AddSingleton<StorageBucketInitializer>();
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' não configurada.");

        services.AddScoped<DomainEventsDispatchInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure();
            });

            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsDispatchInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
    }

    private static void AddIdentityWithBCrypt(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher<ApplicationUser>>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
    }
}