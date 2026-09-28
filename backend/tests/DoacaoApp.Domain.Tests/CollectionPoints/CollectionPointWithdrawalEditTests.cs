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
        var clothingId = TestData.AddCategory(point, "Roupas");
        var footwearId = TestData.AddCategory(point, "Calçados");
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [clothingId], 0, TestData.Now, "Nota original")
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, isMostRecentForBeneficiary: true, [footwearId], "Nota nova", 0);

        result.IsError.Should().BeFalse();
        withdrawal.CategoryIds.Should().Equal(footwearId);
        withdrawal.Notes.Should().Be("Nota nova");
    }

    [Fact]
    public void UpdateWithdrawal_RetiradaInexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();
        var categoryId = TestData.AddCategory(point);

        var result = point.UpdateWithdrawal(
            WithdrawalId.New(), isMostRecentForBeneficiary: true, [categoryId], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.NotFound");
    }

    [Fact]
    public void UpdateWithdrawal_QuandoNaoEAMaisRecenteDoBeneficiario_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddCategory(point, "Roupas");
        var footwearId = TestData.AddCategory(point, "Calçados");
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [clothingId], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, isMostRecentForBeneficiary: false, [footwearId], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.OnlyMostRecentIsEditable");
        withdrawal.CategoryIds.Should().Equal(clothingId);
    }

    [Fact]
    public void UpdateWithdrawal_SemNenhumaCategoria_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddCategory(point, "Roupas");
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [clothingId], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CategoriesRequired");
        withdrawal.CategoryIds.Should().Equal(clothingId);
    }

    [Fact]
    public void UpdateWithdrawal_ComCategoriaInexistenteNoPonto_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddCategory(point, "Roupas");
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [clothingId], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [CategoryId.New()], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
        withdrawal.CategoryIds.Should().Equal(clothingId);
    }

    [Fact]
    public void UpdateWithdrawal_ComCestaUltrapassandoOLimiteDoCiclo_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var foodId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, quantity: 10, categoryId: foodId);
        var clothingId = TestData.AddCategory(point, "Roupas");
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 2));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [clothingId], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [foodId], null, beneficiaryWithdrawalsInCurrentCycleExcludingThis: 1,
            basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        withdrawal.CategoryIds.Should().Equal(clothingId);
        withdrawal.BasketTemplateId.Should().BeNull();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }

    [Fact]
    public void UpdateWithdrawal_MantendoCestaJaContabilizadaNelaMesma_NaoDeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var foodId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, quantity: 10, categoryId: foodId);
        var clothingId = TestData.AddCategory(point, "Roupas");
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 2));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [foodId], 0, TestData.Now, null, basket: (basketId, 1))
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [foodId, clothingId], "Ajustado",
            beneficiaryWithdrawalsInCurrentCycleExcludingThis: 0, basket: (basketId, 1));

        result.IsError.Should().BeFalse();
        withdrawal.CategoryIds.Should().Equal(foodId, clothingId);
        withdrawal.Notes.Should().Be("Ajustado");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(8);
    }

    [Fact]
    public void UpdateWithdrawal_TrocandoDeCesta_DevolveOEstoqueDaAntigaAntesDeValidarANova()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var foodId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, "Arroz", quantity: 10, categoryId: foodId);
        var smallBasketId = TestData.CreateBasketTemplate(point, "Cesta pequena", (riceId, 8));
        var bigBasketId = TestData.CreateBasketTemplate(point, "Cesta grande", (riceId, 10));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [foodId], 0, TestData.Now, null, basket: (smallBasketId, 1))
            .Value;
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(2);

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [foodId], null,
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
        var foodId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, quantity: 10, categoryId: foodId);
        var clothingId = TestData.AddCategory(point, "Roupas");
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 3));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [foodId], 0, TestData.Now, null, basket: (basketId, 1))
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [clothingId], null, 0);

        result.IsError.Should().BeFalse();
        withdrawal.BasketTemplateId.Should().BeNull();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }
}
