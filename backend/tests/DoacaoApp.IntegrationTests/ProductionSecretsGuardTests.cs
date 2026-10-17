using DoacaoApp.Api.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DoacaoApp.IntegrationTests;

public class ProductionSecretsGuardTests
{
    private const string RealConnectionString =
        "Host=postgres;Port=5432;Database=doacao;Username=doacao;Password=s3nh4-real-do-banco";

    private static IConfiguration ProductionLikeConfiguration(params (string Key, string? Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "uma-chave-gerada-de-verdade-com-mais-de-32-caracteres",
            ["Storage:SecretKey"] = "segredo-minio-gerado",
            ["SeedData:Enabled"] = "false",
            ["SeedData:AdminPassword"] = "",
            ["ConnectionStrings:Database"] = RealConnectionString,
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void ConfiguracaoDeProducaoAtual_SeedDesligadoESenhaDoAdminVazia_DeveSubir()
    {
        WebApplicationExtensions.FindDefaultSecrets(ProductionLikeConfiguration()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Jwt:Secret")]
    [InlineData("Storage:SecretKey")]
    public void PlaceholderGerar_EmSegredo_DeveSerRecusado(string key)
    {
        var configuration = ProductionLikeConfiguration((key, "GERAR"));

        WebApplicationExtensions.FindDefaultSecrets(configuration).Should().ContainSingle().Which.Should().Be(key);
    }

    [Fact]
    public void PlaceholderGerar_NaSenhaDoAdmin_ComSeedLigado_DeveSerRecusado()
    {
        var configuration = ProductionLikeConfiguration(("SeedData:Enabled", "true"), ("SeedData:AdminPassword", "GERAR"));

        WebApplicationExtensions.FindDefaultSecrets(configuration).Should().Equal("SeedData:AdminPassword");
    }

    [Fact]
    public void PlaceholderGerar_NaSenhaDoAdmin_ComSeedDesligado_DeveSerIgnorado()
    {
        var configuration = ProductionLikeConfiguration(("SeedData:AdminPassword", "GERAR"));

        WebApplicationExtensions.FindDefaultSecrets(configuration).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Host=postgres;Database=doacao;Username=doacao;Password=GERAR")]
    [InlineData("Host=postgres;Database=doacao;Username=doacao;password=GERAR;Port=5432")]
    [InlineData("Host=postgres;Database=doacao;Username=doacao;Password=troque-esta-senha-em-producao")]
    public void PlaceholderNaSenhaDaConnectionString_DeveSerRecusado(string connectionString)
    {
        var configuration = ProductionLikeConfiguration(("ConnectionStrings:Database", connectionString));

        WebApplicationExtensions.FindDefaultSecrets(configuration).Should().Equal("ConnectionStrings:Database");
    }

    [Fact]
    public void GerarComoParteDeOutroValor_NaoDeveSerConfundidoComPlaceholder()
    {
        var configuration = ProductionLikeConfiguration(
            ("ConnectionStrings:Database", "Host=postgres;Database=GERAR_DB;Username=doacao;Password=GERARxyz123"));

        WebApplicationExtensions.FindDefaultSecrets(configuration).Should().BeEmpty();
    }
}
