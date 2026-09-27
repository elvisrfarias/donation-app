using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointDivulgacaoTests
{
    [Fact]
    public void Create_NormalizaNecessidades_TrimDedupELimite()
    {
        var needs = new[] { "  Fraldas  ", "Fraldas", "Leite", "" };

        var point = CollectionPoint.Create(
            "Ponto", CollectionPointType.Association, TestData.Address(), TestData.Contact(), TestData.Now,
            needs: needs).Value;

        point.Needs.Should().BeEquivalentTo("Fraldas", "Leite");
    }

    [Fact]
    public void UpdateOpeningHours_Vazio_ViraNulo()
    {
        var point = TestData.Point();

        point.UpdateOpeningHours("   ");

        point.OpeningHours.Should().BeNull();
    }

    [Fact]
    public void UpdateNeeds_SubstituiListaAtual()
    {
        var point = TestData.Point();
        point.UpdateNeeds(new[] { "Arroz" });

        point.UpdateNeeds(new[] { "Feijão", "Óleo" });

        point.Needs.Should().BeEquivalentTo("Feijão", "Óleo");
    }
}
