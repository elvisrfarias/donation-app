using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.CollectionPoints.Events;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointWithdrawalTests
{
    private static (CollectionPoint Point, StockItemId RiceId, BasketTemplateId BasketId) PointWithBasket(
        int maxPerCycle = 1, int riceQuantity = 10, int quantityPerBasket = 2)
    {
        var point = TestData.Point(maxPerCycle: maxPerCycle);
        var riceId = TestData.AddStockItem(point, "Arroz", "kg", riceQuantity);
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, quantityPerBasket));
        return (point, riceId, basketId);
    }

    [Fact]
    public void RegisterWithdrawal_ComCestaDentroDoLimite_DeveRegistrarEDescontarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(maxPerCycle: 1, riceQuantity: 10, quantityPerBasket: 2);
        var categoryId = point.Categories[0].Id;

        var result = point.RegisterWithdrawal(
            beneficiary, [categoryId], beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null,
            basket: (basketId, 1));

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(8);
    }

    [Fact]
    public void RegisterWithdrawal_ComCestaQuandoBeneficiarioJaAtingiuOLimite_DeveBloquearEManterEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(maxPerCycle: 1);
        var categoryId = point.Categories[0].Id;

        var result = point.RegisterWithdrawal(
            beneficiary, [categoryId], beneficiaryWithdrawalsInCurrentCycle: 1, TestData.Now, null,
            basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComCestaAteOTeto_DevePermitir()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, _, basketId) = PointWithBasket(maxPerCycle: 3, riceQuantity: 100, quantityPerBasket: 1);
        var categoryId = point.Categories[0].Id;

        point.RegisterWithdrawal(beneficiary, [categoryId], 2, TestData.Now, null, basket: (basketId, 1))
            .IsError.Should().BeFalse();
        point.RegisterWithdrawal(beneficiary, [categoryId], 3, TestData.Now, null, basket: (basketId, 1))
            .IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData("Roupas")]
    [InlineData("Calçados")]
    [InlineData("Higiene")]
    [InlineData("Brinquedos")]
    public void RegisterWithdrawal_SemCesta_NaoAplicaOLimiteDoCicloMesmoComACotaEsgotada(string categoryName)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.AddCategory(point, categoryName);

        var result = point.RegisterWithdrawal(beneficiary, [categoryId], beneficiaryWithdrawalsInCurrentCycle: 50, TestData.Now, null);

        result.IsError.Should().BeFalse();
        result.Value.CategoryIds.Should().Equal(categoryId);
        result.Value.CountsTowardsLimit.Should().BeFalse();
    }

    [Fact]
    public void RegisterWithdrawal_GuardaACategoriaDaRetirada()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var categoryId = TestData.AddCategory(point, "Roupas");

        var result = point.RegisterWithdrawal(beneficiary, [categoryId], 0, TestData.Now, null);

        result.Value.CategoryIds.Should().Equal(categoryId);
        point.DomainEvents.OfType<WithdrawalRegistered>().Single().CategoryIds.Should().Equal(categoryId);
    }

    [Fact]
    public void RegisterWithdrawal_ComVariasCategorias_GuardaTodasSemRepeticao()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var foodId = TestData.AddCategory(point, "Alimentos");
        var clothingId = TestData.AddCategory(point, "Roupas");
        var footwearId = TestData.AddCategory(point, "Calçados");

        var result = point.RegisterWithdrawal(
            beneficiary,
            [foodId, clothingId, footwearId, clothingId],
            0,
            TestData.Now,
            null);

        result.Value.CategoryIds.Should().Equal(foodId, clothingId, footwearId);
    }

    [Fact]
    public void RegisterWithdrawal_SemNenhumaCategoria_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(beneficiary, [], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CategoriesRequired");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComCategoriaInexistenteNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(beneficiary, [CategoryId.New()], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComEstoqueInsuficienteParaACesta_DeveBloquearSemMutarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(riceQuantity: 3, quantityPerBasket: 2);
        var categoryId = point.Categories[0].Id;

        var result = point.RegisterWithdrawal(
            beneficiary, [categoryId], 0, TestData.Now, null, basket: (basketId, 2));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(3);
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComEstoqueExatamenteSuficienteParaACesta_DevePermitir()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(riceQuantity: 4, quantityPerBasket: 2);
        var categoryId = point.Categories[0].Id;

        var result = point.RegisterWithdrawal(
            beneficiary, [categoryId], 0, TestData.Now, null, basket: (basketId, 2));

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(0);
    }

    [Fact]
    public void RegisterWithdrawal_ComReceitaInativa_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket();
        var categoryId = point.Categories[0].Id;
        point.UpdateBasketTemplate(basketId, "Cesta básica", [(riceId, 2)], isActive: false);

        var result = point.RegisterWithdrawal(beneficiary, [categoryId], 0, TestData.Now, null, basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateInactive");
    }

    [Fact]
    public void RegisterWithdrawal_ComReceitaInexistente_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var categoryId = TestData.AddCategory(point);

        var result = point.RegisterWithdrawal(
            beneficiary, [categoryId], 0, TestData.Now, null, basket: (BasketTemplateId.New(), 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateNotFound");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoPontoInativo_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var categoryId = TestData.AddCategory(point);
        point.Deactivate();

        var result = point.RegisterWithdrawal(beneficiary, [categoryId], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.Inactive");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoSucesso_DeveDispararEvento()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var categoryId = TestData.AddCategory(point);

        point.RegisterWithdrawal(beneficiary, [categoryId], 0, TestData.Now, "Cesta básica");

        point.DomainEvents.Should().ContainSingle(e => e is WithdrawalRegistered);
    }

    [Fact]
    public void RegisterWithdrawal_UsaOLimiteDoPontoOndeARetiradaAcontece_NaoODeOutroPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var (strictPoint, _, strictBasketId) = PointWithBasket(maxPerCycle: 1);
        var (generousPoint, _, generousBasketId) = PointWithBasket(maxPerCycle: 3);

        var atStrict = strictPoint.RegisterWithdrawal(
            beneficiary, [strictPoint.Categories[0].Id], 1, TestData.Now, null, basket: (strictBasketId, 1));
        var atGenerous = generousPoint.RegisterWithdrawal(
            beneficiary, [generousPoint.Categories[0].Id], 1, TestData.Now, null, basket: (generousBasketId, 1));

        atStrict.IsError.Should().BeTrue();
        atGenerous.IsError.Should().BeFalse();
    }
}
