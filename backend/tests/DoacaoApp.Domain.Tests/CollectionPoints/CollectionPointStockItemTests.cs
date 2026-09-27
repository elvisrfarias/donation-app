using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointStockItemTests
{
    [Fact]
    public void NovoPonto_NaoTemItensDeEstoque()
    {
        var point = TestData.Point();

        point.StockItems.Should().BeEmpty();
    }

    [Fact]
    public void AddStockItem_Valido_DeveAdicionarOItem()
    {
        var point = TestData.Point();

        var result = point.AddStockItem("Arroz", "kg", ItemCategory.Food, initialQuantity: 20, goal: 50, alertThreshold: 10, isTracked: true);

        result.IsError.Should().BeFalse();
        var item = point.StockItems.Single(i => i.Id == result.Value);
        item.Name.Should().Be("Arroz");
        item.Unit.Should().Be("kg");
        item.Category.Should().Be(ItemCategory.Food);
        item.Quantity.Should().Be(20);
        item.Goal.Should().Be(50);
        item.AlertThreshold.Should().Be(10);
        item.IsTracked.Should().BeTrue();
    }

    [Fact]
    public void AddStockItem_ComNomeVazio_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.AddStockItem("   ", "kg", ItemCategory.Food, 0, 0, 0, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNameRequired");
    }

    [Fact]
    public void AddStockItem_ComUnidadeVazia_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.AddStockItem("Arroz", "   ", ItemCategory.Food, 0, 0, 0, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemUnitRequired");
    }

    [Fact]
    public void AddStockItem_ComNomeDuplicadoIgnorandoMaiusculasEMinusculas_DeveBloquear()
    {
        var point = TestData.Point();
        point.AddStockItem("Arroz", "kg", ItemCategory.Food, 0, 0, 0, true);

        var result = point.AddStockItem("ARROZ", "kg", ItemCategory.Food, 0, 0, 0, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.DuplicateStockItemName");
        point.StockItems.Should().HaveCount(1);
    }

    [Fact]
    public void AddStockItem_ComQuantidadeNegativa_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.AddStockItem("Arroz", "kg", ItemCategory.Food, initialQuantity: -1, 0, 0, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.NegativeQuantity");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void AddStockItem_ComMetaOuAvisoNegativo_DeveBloquear(int goal, int alertThreshold)
    {
        var point = TestData.Point();

        var result = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 0, goal, alertThreshold, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StockSetting.NegativeValue");
    }

    [Fact]
    public void AddStockItem_ComAvisoAcimaDaMeta_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 0, goal: 10, alertThreshold: 11, isTracked: true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StockSetting.AlertAboveGoal");
    }

    [Fact]
    public void UpdateStockItem_Valido_DeveAtualizarMetadadosSemAlterarAQuantidade()
    {
        var point = TestData.Point();
        var itemId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 20, 0, 0, true).Value;

        var result = point.UpdateStockItem(
            itemId, "Arroz Tipo 1", "pacote", ItemCategory.Food, goal: 30, alertThreshold: 5, isTracked: false);

        result.IsError.Should().BeFalse();
        var item = point.StockItems.Single(i => i.Id == itemId);
        item.Name.Should().Be("Arroz Tipo 1");
        item.Unit.Should().Be("pacote");
        item.Goal.Should().Be(30);
        item.AlertThreshold.Should().Be(5);
        item.IsTracked.Should().BeFalse();
        item.Quantity.Should().Be(20);
    }

    [Fact]
    public void UpdateStockItem_ItemInexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();

        var result = point.UpdateStockItem(StockItemId.New(), "Arroz", "kg", ItemCategory.Food, 0, 0, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public void UpdateStockItem_ComNomeDuplicadoDeOutroItem_DeveBloquear()
    {
        var point = TestData.Point();
        point.AddStockItem("Arroz", "kg", ItemCategory.Food, 0, 0, 0, true);
        var sugarId = point.AddStockItem("Açúcar", "kg", ItemCategory.Food, 0, 0, 0, true).Value;

        var result = point.UpdateStockItem(sugarId, "arroz", "kg", ItemCategory.Food, 0, 0, true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.DuplicateStockItemName");
    }

    [Fact]
    public void UpdateStockItem_MantendoOMesmoNome_NaoBloqueiaPorDuplicidade()
    {
        var point = TestData.Point();
        var itemId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 0, 0, 0, true).Value;

        var result = point.UpdateStockItem(itemId, "Arroz", "kg", ItemCategory.Food, 10, 5, true);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void RemoveStockItem_NaoReferenciadoEmReceita_DeveRemover()
    {
        var point = TestData.Point();
        var itemId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 0, 0, 0, true).Value;

        var result = point.RemoveStockItem(itemId);

        result.IsError.Should().BeFalse();
        point.StockItems.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStockItem_ReferenciadoEmReceita_DeveBloquear()
    {
        var point = TestData.Point();
        var itemId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;
        point.CreateBasketTemplate("Cesta básica", [(itemId, 1)]);

        var result = point.RemoveStockItem(itemId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemInUseByBasketTemplate");
        point.StockItems.Should().ContainSingle(i => i.Id == itemId);
    }

    [Fact]
    public void RemoveStockItem_ItemInexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();

        var result = point.RemoveStockItem(StockItemId.New());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public void ReplaceStockQuantities_Valido_DeveAtualizarAsQuantidades()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;
        var sugarId = point.AddStockItem("Açúcar", "kg", ItemCategory.Food, 5, 0, 0, true).Value;

        var result = point.ReplaceStockQuantities(new Dictionary<StockItemId, int> { [riceId] = 30, [sugarId] = 12 });

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(30);
        point.StockItems.Single(i => i.Id == sugarId).Quantity.Should().Be(12);
    }

    [Fact]
    public void ReplaceStockQuantities_ComItemInexistente_DeveBloquearSemAlterarNada()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;

        var result = point.ReplaceStockQuantities(new Dictionary<StockItemId, int> { [riceId] = 30, [StockItemId.New()] = 5 });

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }

    [Fact]
    public void ReplaceStockQuantities_ComQuantidadeNegativa_DeveBloquearSemAlterarNada()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;

        var result = point.ReplaceStockQuantities(new Dictionary<StockItemId, int> { [riceId] = -1 });

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.NegativeQuantity");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }

    [Fact]
    public void ReplaceStockItemSettings_Valido_DeveAtualizarMetaAvisoEMonitoramento()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;

        var result = point.ReplaceStockItemSettings(
            new Dictionary<StockItemId, (int Goal, int AlertThreshold, bool IsTracked)> { [riceId] = (50, 10, false) });

        result.IsError.Should().BeFalse();
        var item = point.StockItems.Single(i => i.Id == riceId);
        item.Goal.Should().Be(50);
        item.AlertThreshold.Should().Be(10);
        item.IsTracked.Should().BeFalse();
    }

    [Fact]
    public void ReplaceStockItemSettings_ComItemInexistente_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.ReplaceStockItemSettings(
            new Dictionary<StockItemId, (int Goal, int AlertThreshold, bool IsTracked)> { [StockItemId.New()] = (10, 5, true) });

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public void ReplaceStockItemSettings_ComAvisoAcimaDaMeta_DeveBloquear()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;

        var result = point.ReplaceStockItemSettings(
            new Dictionary<StockItemId, (int Goal, int AlertThreshold, bool IsTracked)> { [riceId] = (10, 11, true) });

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StockSetting.AlertAboveGoal");
    }
}
