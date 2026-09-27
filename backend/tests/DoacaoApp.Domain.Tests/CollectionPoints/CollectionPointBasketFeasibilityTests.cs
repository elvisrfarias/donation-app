using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointBasketFeasibilityTests
{
    [Fact]
    public void CalculateBasketFeasibility_QuandoUmItemEOGargalo_LimitaOTotalDeCestas()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 25, 0, 0, true).Value;
        var sugarId = point.AddStockItem("Açúcar", "kg", ItemCategory.Food, 4, 0, 0, true).Value;
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 5), (sugarId, 2)]).Value;

        var feasibility = point.CalculateBasketFeasibility(templateId);

        feasibility.CompleteBaskets.Should().Be(2);
    }

    [Fact]
    public void CalculateBasketFeasibility_CalculaOQueFaltaParaAProximaCesta()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 25, 0, 0, true).Value;
        var sugarId = point.AddStockItem("Açúcar", "kg", ItemCategory.Food, 4, 0, 0, true).Value;
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 5), (sugarId, 2)]).Value;

        var feasibility = point.CalculateBasketFeasibility(templateId);

        feasibility.Missing.Should().BeEquivalentTo(
        [
            new BasketItemGap(riceId, "Arroz", 5, 25, 0),
            new BasketItemGap(sugarId, "Açúcar", 2, 4, 2),
        ]);
    }

    [Fact]
    public void CalculateBasketFeasibility_QuandoUmItemEstaZerado_RetornaZeroCestas()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;
        var sugarId = point.AddStockItem("Açúcar", "kg", ItemCategory.Food, 0, 0, 0, true).Value;
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 5), (sugarId, 1)]).Value;

        var feasibility = point.CalculateBasketFeasibility(templateId);

        feasibility.CompleteBaskets.Should().Be(0);
        feasibility.Missing.Should().Contain(gap => gap.ItemId == sugarId && gap.NeededForNextBasket == 1);
    }

    [Fact]
    public void CalculateBasketFeasibility_ReceitaInexistente_RetornaZeroSemItens()
    {
        var point = TestData.Point();

        var feasibility = point.CalculateBasketFeasibility(BasketTemplateId.New());

        feasibility.CompleteBaskets.Should().Be(0);
        feasibility.Missing.Should().BeEmpty();
    }
}
