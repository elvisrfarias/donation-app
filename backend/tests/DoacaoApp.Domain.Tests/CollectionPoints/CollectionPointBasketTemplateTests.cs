using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointBasketTemplateTests
{
    private static (CollectionPoint Point, StockItemId RiceId, StockItemId SugarId) PointWithItems()
    {
        var point = TestData.Point();
        var riceId = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 0, true).Value;
        var sugarId = point.AddStockItem("Açúcar", "kg", ItemCategory.Food, 5, 0, 0, true).Value;
        return (point, riceId, sugarId);
    }

    [Fact]
    public void CreateBasketTemplate_Valido_DeveCriarAtivaEComOsItens()
    {
        var (point, riceId, sugarId) = PointWithItems();

        var result = point.CreateBasketTemplate("Cesta básica", [(riceId, 2), (sugarId, 1)]);

        result.IsError.Should().BeFalse();
        var template = point.BasketTemplates.Single(t => t.Id == result.Value);
        template.Name.Should().Be("Cesta básica");
        template.IsActive.Should().BeTrue();
        template.Items.Should().BeEquivalentTo(
        [
            new BasketTemplateItem(riceId, 2),
            new BasketTemplateItem(sugarId, 1),
        ]);
    }

    [Fact]
    public void CreateBasketTemplate_ComNomeVazio_DeveBloquear()
    {
        var (point, riceId, _) = PointWithItems();

        var result = point.CreateBasketTemplate("   ", [(riceId, 1)]);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateNameRequired");
    }

    [Fact]
    public void CreateBasketTemplate_SemItens_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.CreateBasketTemplate("Cesta básica", []);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateItemsRequired");
    }

    [Fact]
    public void CreateBasketTemplate_ComItemInexistenteNoPonto_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.CreateBasketTemplate("Cesta básica", [(StockItemId.New(), 1)]);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public void CreateBasketTemplate_ComItemDuplicado_DeveBloquear()
    {
        var (point, riceId, _) = PointWithItems();

        var result = point.CreateBasketTemplate("Cesta básica", [(riceId, 2), (riceId, 3)]);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateDuplicateItem");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateBasketTemplate_ComQuantidadeNaoPositiva_DeveBloquear(int quantity)
    {
        var (point, riceId, _) = PointWithItems();

        var result = point.CreateBasketTemplate("Cesta básica", [(riceId, quantity)]);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateInvalidQuantity");
    }

    [Fact]
    public void UpdateBasketTemplate_Valido_DeveAtualizarNomeItensEStatus()
    {
        var (point, riceId, sugarId) = PointWithItems();
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 2)]).Value;

        var result = point.UpdateBasketTemplate(templateId, "Cesta reforçada", [(riceId, 3), (sugarId, 1)], isActive: false);

        result.IsError.Should().BeFalse();
        var template = point.BasketTemplates.Single(t => t.Id == templateId);
        template.Name.Should().Be("Cesta reforçada");
        template.IsActive.Should().BeFalse();
        template.Items.Should().BeEquivalentTo(
        [
            new BasketTemplateItem(riceId, 3),
            new BasketTemplateItem(sugarId, 1),
        ]);
    }

    [Fact]
    public void UpdateBasketTemplate_Inexistente_DeveRetornarNotFound()
    {
        var (point, riceId, _) = PointWithItems();

        var result = point.UpdateBasketTemplate(BasketTemplateId.New(), "Cesta", [(riceId, 1)], true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateNotFound");
    }

    [Fact]
    public void UpdateBasketTemplate_ComItemInexistenteNoPonto_DeveBloquear()
    {
        var (point, riceId, _) = PointWithItems();
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 2)]).Value;

        var result = point.UpdateBasketTemplate(templateId, "Cesta básica", [(StockItemId.New(), 1)], true);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public void RemoveBasketTemplate_Existente_DeveRemover()
    {
        var (point, riceId, _) = PointWithItems();
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 1)]).Value;

        var result = point.RemoveBasketTemplate(templateId);

        result.IsError.Should().BeFalse();
        point.BasketTemplates.Should().BeEmpty();
    }

    [Fact]
    public void RemoveBasketTemplate_Inexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();

        var result = point.RemoveBasketTemplate(BasketTemplateId.New());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateNotFound");
    }

    [Fact]
    public void RemoveBasketTemplate_JaUsadaEmRetirada_DeveBloquear()
    {
        var (point, riceId, _) = PointWithItems();
        var templateId = point.CreateBasketTemplate("Cesta básica", [(riceId, 1)]).Value;
        var beneficiary = TestData.Beneficiary();
        point.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null,
            basket: (templateId, 1));

        var result = point.RemoveBasketTemplate(templateId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateInUseByWithdrawal");
        point.BasketTemplates.Should().ContainSingle(t => t.Id == templateId);
    }
}
