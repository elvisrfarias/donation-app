using DoacaoApp.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace DoacaoApp.IntegrationTests;

/// <summary>
/// Sobe um PostgreSQL real (Testcontainers) e configura a aplicação para usá-lo.
/// As migrations e o seed do admin rodam no startup do host. O object storage é
/// substituído por um fake em memória (não há MinIO nos testes).
/// </summary>
public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("doacao_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("RunMigrationsOnStartup", "true");
        builder.UseSetting("SeedData:Enabled", "true");
        builder.UseSetting("SeedData:AdminEmail", "admin@doacao.local");
        builder.UseSetting("SeedData:AdminPassword", "Admin@123456");
        builder.UseSetting("Jwt:Secret", "CHAVE-DE-TESTE-COM-NO-MINIMO-32-CARACTERES-12345");
        builder.UseSetting("Storage:ServiceUrl", "http://127.0.0.1:1");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage, FakeFileStorage>();
        });
    }

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}