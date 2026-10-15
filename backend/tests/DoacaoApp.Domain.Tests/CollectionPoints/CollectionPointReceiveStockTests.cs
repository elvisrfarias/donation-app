using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointReceiveStockTests
{
    [Fact]
    public void ReceiveStock_SomaNaQuantidadeExistente()
    {
        var point = TestData.Point();
        var rice = TestData.AddStockItem(point, "Arroz", quantity: 10);
        var beans = TestData.AddStockItem(point, "Feijão", quantity: 5);

        var result = point.ReceiveStock(new Dictionary<StockItemId, int> { [rice] = 4, [beans] = 1 });

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == rice).Quantity.Should().Be(14);
        point.StockItems.Single(i => i.Id == beans).Quantity.Should().Be(6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ReceiveStock_ComQuantidadeNaoPositiva_DeveFalharSemAlterarNada(int quantity)
    {
        var point = TestData.Point();
        var rice = TestData.AddStockItem(point, "Arroz", quantity: 10);

        var result = point.ReceiveStock(new Dictionary<StockItemId, int> { [rice] = quantity });

        result.FirstError.Code.Should().Be("CollectionPoint.NonPositiveQuantity");
        point.StockItems.Single().Quantity.Should().Be(10);
    }

    [Fact]
    public void ReceiveStock_ComItemInexistente_DeveFalharSemAlterarNada()
    {
        var point = TestData.Point();
        var rice = TestData.AddStockItem(point, "Arroz", quantity: 10);

        var result = point.ReceiveStock(new Dictionary<StockItemId, int> { [rice] = 2, [StockItemId.New()] = 1 });

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
        point.StockItems.Single().Quantity.Should().Be(10);
    }
}
