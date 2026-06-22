using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace DoacaoApp.IntegrationTests;

/// <summary>
/// Sobe um PostgreSQL real (Testcontainers) e configura a aplicação para usá-lo.
/// As migrations e o seed do admin rodam no startup do host.
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

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _database.GetConnectionString(),
                ["RunMigrationsOnStartup"] = "true",
                ["SeedData:Enabled"] = "true",
                ["SeedData:AdminEmail"] = "admin@doacao.local",
                ["SeedData:AdminPassword"] = "Admin@123456",
                ["Jwt:Secret"] = "CHAVE-DE-TESTE-COM-NO-MINIMO-32-CARACTERES-12345",
            });
        });
    }

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}