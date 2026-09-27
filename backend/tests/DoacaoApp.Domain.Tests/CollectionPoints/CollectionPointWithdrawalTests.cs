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

        var result = point.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null,
            basket: (basketId, 1));

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(8);
    }

    [Fact]
    public void RegisterWithdrawal_ComCestaQuandoBeneficiarioJaAtingiuOLimite_DeveBloquearEManterEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], beneficiaryWithdrawalsInCurrentCycle: 1, TestData.Now, null,
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

        point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 2, TestData.Now, null, basket: (basketId, 1))
            .IsError.Should().BeFalse();
        point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 3, TestData.Now, null, basket: (basketId, 1))
            .IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(ItemCategory.Clothing)]
    [InlineData(ItemCategory.Footwear)]
    [InlineData(ItemCategory.Hygiene)]
    [InlineData(ItemCategory.Toys)]
    public void RegisterWithdrawal_SemCesta_NaoAplicaOLimiteDoCicloMesmoComACotaEsgotada(ItemCategory category)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, [category], beneficiaryWithdrawalsInCurrentCycle: 50, TestData.Now, null);

        result.IsError.Should().BeFalse();
        result.Value.Categories.Should().Equal(category);
        result.Value.CountsTowardsLimit.Should().BeFalse();
    }

    [Fact]
    public void RegisterWithdrawal_GuardaACategoriaDaRetirada()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null);

        result.Value.Categories.Should().Equal(ItemCategory.Clothing);
        point.DomainEvents.OfType<WithdrawalRegistered>().Single().Categories.Should().Equal(ItemCategory.Clothing);
    }

    [Fact]
    public void RegisterWithdrawal_ComVariasCategorias_GuardaTodasSemRepeticao()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(
            beneficiary,
            [ItemCategory.Food, ItemCategory.Clothing, ItemCategory.Footwear, ItemCategory.Clothing],
            0,
            TestData.Now,
            null);

        result.Value.Categories.Should().Equal(ItemCategory.Food, ItemCategory.Clothing, ItemCategory.Footwear);
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
    public void RegisterWithdrawal_ComEstoqueInsuficienteParaACesta_DeveBloquearSemMutarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(riceQuantity: 3, quantityPerBasket: 2);

        var result = point.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (basketId, 2));

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

        var result = point.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (basketId, 2));

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(0);
    }

    [Fact]
    public void RegisterWithdrawal_ComReceitaInativa_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket();
        point.UpdateBasketTemplate(basketId, "Cesta básica", [(riceId, 2)], isActive: false);

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateInactive");
    }

    [Fact]
    public void RegisterWithdrawal_ComReceitaInexistente_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], 0, TestData.Now, null, basket: (BasketTemplateId.New(), 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateNotFound");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoPontoInativo_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.Deactivate();

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.Inactive");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoSucesso_DeveDispararEvento()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, "Cesta básica");

        point.DomainEvents.Should().ContainSingle(e => e is WithdrawalRegistered);
    }

    [Fact]
    public void RegisterWithdrawal_UsaOLimiteDoPontoOndeARetiradaAcontece_NaoODeOutroPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var (strictPoint, _, strictBasketId) = PointWithBasket(maxPerCycle: 1);
        var (generousPoint, _, generousBasketId) = PointWithBasket(maxPerCycle: 3);

        var atStrict = strictPoint.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], 1, TestData.Now, null, basket: (strictBasketId, 1));
        var atGenerous = generousPoint.RegisterWithdrawal(
            beneficiary, [ItemCategory.Food], 1, TestData.Now, null, basket: (generousBasketId, 1));

        atStrict.IsError.Should().BeTrue();
        atGenerous.IsError.Should().BeFalse();
    }
}
