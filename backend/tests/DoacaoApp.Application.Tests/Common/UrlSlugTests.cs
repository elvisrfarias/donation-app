using DoacaoApp.Application.Common.Text;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Application.Tests.Common;

public class UrlSlugTests
{
    [Theory]
    [InlineData("Casa Solidária", "Concórdia", "SC", "casa-solidaria-concordia-sc")]
    [InlineData("  Paróquia São José (Centro) ", "Itá", "SC", "paroquia-sao-jose-centro-ita-sc")]
    [InlineData("Associação Ação & Vida — 24h", "São Paulo", "SP", "associacao-acao-vida-24h-sao-paulo-sp")]
    public void From_JuntaAsPartesSemAcentosNemSimbolos(string name, string city, string state, string expected)
    {
        UrlSlug.From(name, city, state).Should().Be(expected);
    }

    [Fact]
    public void From_IgnoraPartesVazias()
    {
        UrlSlug.From("Igreja", null, " ", "SC").Should().Be("igreja-sc");
    }

    [Fact]
    public void From_LimitaOTamanhoSemTerminarEmHifen()
    {
        var slug = UrlSlug.From(string.Join(' ', Enumerable.Repeat("palavra", 30)));

        slug.Length.Should().BeLessThanOrEqualTo(UrlSlug.MaxLength);
        slug.Should().NotEndWith("-");
    }
}
