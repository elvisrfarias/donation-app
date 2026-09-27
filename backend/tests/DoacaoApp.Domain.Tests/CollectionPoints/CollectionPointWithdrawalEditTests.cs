using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointWithdrawalEditTests
{
    [Fact]
    public void UpdateWithdrawal_ComDadosValidos_DeveAlterarCategoriasENotas()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, "Nota original")
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, isMostRecentForBeneficiary: true, [ItemCategory.Footwear], "Nota nova", 0);

        result.IsError.Should().BeFalse();
        withdrawal.Categories.Should().Equal(ItemCategory.Footwear);
        withdrawal.Notes.Should().Be("Nota nova");
    }

    [Fact]
    public void UpdateWithdrawal_RetiradaInexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();

        var result = point.UpdateWithdrawal(
            WithdrawalId.New(), isMostRecentForBeneficiary: true, [ItemCategory.Food], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.NotFound");
    }

    [Fact]
    public void UpdateWithdrawal_QuandoNaoEAMaisRecenteDoBeneficiario_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, isMostRecentForBeneficiary: false, [ItemCategory.Footwear], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.OnlyMostRecentIsEditable");
        withdrawal.Categories.Should().Equal(ItemCategory.Clothing);
    }

    [Fact]
    public void UpdateWithdrawal_SemNenhumaCategoria_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CategoriesRequired");
        withdrawal.Categories.Should().Equal(ItemCategory.Clothing);
    }

    [Fact]
    public void UpdateWithdrawal_ComCestaUltrapassandoOLimiteDoCiclo_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var riceId = TestData.AddStockItem(point, quantity: 10);
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 2));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [ItemCategory.Food], null, beneficiaryWithdrawalsInCurrentCycleExcludingThis: 1,
            basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        withdrawal.Categories.Should().Equal(ItemCategory.Clothing);
        withdrawal.BasketTemplateId.Should().BeNull();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }

    [Fact]
    public void UpdateWithdrawal_MantendoCestaJaContabilizadaNelaMesma_NaoDeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var riceId = TestData.AddStockItem(point, quantity: 10);
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 2));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (basketId, 1))
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [ItemCategory.Food, ItemCategory.Clothing], "Ajustado",
            beneficiaryWithdrawalsInCurrentCycleExcludingThis: 0, basket: (basketId, 1));

        result.IsError.Should().BeFalse();
        withdrawal.Categories.Should().Equal(ItemCategory.Food, ItemCategory.Clothing);
        withdrawal.Notes.Should().Be("Ajustado");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(8);
    }

    [Fact]
    public void UpdateWithdrawal_TrocandoDeCesta_DevolveOEstoqueDaAntigaAntesDeValidarANova()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var riceId = TestData.AddStockItem(point, "Arroz", quantity: 10);
        var smallBasketId = TestData.CreateBasketTemplate(point, "Cesta pequena", (riceId, 8));
        var bigBasketId = TestData.CreateBasketTemplate(point, "Cesta grande", (riceId, 10));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (smallBasketId, 1))
            .Value;
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(2);

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [ItemCategory.Food], null,
            beneficiaryWithdrawalsInCurrentCycleExcludingThis: 0, basket: (bigBasketId, 1));

        result.IsError.Should().BeFalse();
        withdrawal.BasketTemplateId.Should().Be(bigBasketId);
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(0);
    }

    [Fact]
    public void UpdateWithdrawal_RemovendoACesta_DevolveOEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var riceId = TestData.AddStockItem(point, quantity: 10);
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 3));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (basketId, 1))
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [ItemCategory.Clothing], null, 0);

        result.IsError.Should().BeFalse();
        withdrawal.BasketTemplateId.Should().BeNull();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }
}
