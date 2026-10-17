using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Sponsors;

public class SponsorTests
{
    private static readonly StoredFileId Logo = StoredFileId.New();

    [Fact]
    public void Create_Valido_ComecaAtivoComDadosLimpos()
    {
        var result = Sponsor.Create(" Paróquia São José ", SponsorTier.Diamond, " https://paroquia.org.br ", Logo, TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Paróquia São José");
        result.Value.Tier.Should().Be(SponsorTier.Diamond);
        result.Value.WebsiteUrl.Should().Be("https://paroquia.org.br");
        result.Value.LogoFileId.Should().Be(Logo);
        result.Value.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_SemSite_DeixaOSiteVazio(string? websiteUrl)
    {
        Sponsor.Create("Igreja", SponsorTier.Silver, websiteUrl, Logo, TestData.Now).Value.WebsiteUrl.Should().BeNull();
    }

    [Fact]
    public void Create_SemNome_DeveFalhar()
    {
        Sponsor.Create(" ", SponsorTier.Gold, null, Logo, TestData.Now).FirstError.Code.Should().Be("Sponsor.NameRequired");
    }

    [Fact]
    public void Create_ComNomeLongo_DeveFalhar()
    {
        var name = new string('a', Sponsor.MaxNameLength + 1);

        Sponsor.Create(name, SponsorTier.Gold, null, Logo, TestData.Now).FirstError.Code.Should().Be("Sponsor.NameTooLong");
    }

    [Theory]
    [InlineData("paroquia.org.br")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://paroquia.org.br")]
    public void Create_ComSiteInvalido_DeveFalhar(string websiteUrl)
    {
        Sponsor.Create("Igreja", SponsorTier.Gold, websiteUrl, Logo, TestData.Now)
            .FirstError.Code.Should().Be("Sponsor.InvalidWebsiteUrl");
    }

    [Fact]
    public void Create_ComCategoriaInexistente_DeveFalhar()
    {
        Sponsor.Create("Igreja", (SponsorTier)99, null, Logo, TestData.Now).FirstError.Code.Should().Be("Sponsor.InvalidTier");
    }

    [Fact]
    public void UpdateDetails_TrocaCategoriaEDesativa()
    {
        var sponsor = Sponsor.Create("Igreja", SponsorTier.Silver, null, Logo, TestData.Now).Value;

        var result = sponsor.UpdateDetails("Igreja Matriz", SponsorTier.Gold, "https://matriz.org", isActive: false);

        result.IsError.Should().BeFalse();
        sponsor.Name.Should().Be("Igreja Matriz");
        sponsor.Tier.Should().Be(SponsorTier.Gold);
        sponsor.WebsiteUrl.Should().Be("https://matriz.org");
        sponsor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_Invalido_NaoAlteraNada()
    {
        var sponsor = Sponsor.Create("Igreja", SponsorTier.Silver, null, Logo, TestData.Now).Value;

        sponsor.UpdateDetails(" ", SponsorTier.Diamond, null, isActive: false).IsError.Should().BeTrue();

        sponsor.Name.Should().Be("Igreja");
        sponsor.Tier.Should().Be(SponsorTier.Silver);
        sponsor.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Categorias_EstaoOrdenadasDoMaiorParaOMenorPatrocinio()
    {
        Enum.GetValues<SponsorTier>().Should().BeInAscendingOrder()
            .And.ContainInOrder(SponsorTier.Diamond, SponsorTier.Gold, SponsorTier.Silver);
    }
}
