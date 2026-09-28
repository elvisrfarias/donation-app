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

    private static StockItemId AddFreeItem(CollectionPoint point, string name = "Camiseta", int quantity = 100) =>
        TestData.AddStockItem(point, name, "un", quantity);

    [Fact]
    public void RegisterWithdrawal_ComCestaDentroDoLimite_DeveRegistrarEDescontarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(maxPerCycle: 1, riceQuantity: 10, quantityPerBasket: 2);
        var clothingId = AddFreeItem(point);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(clothingId)], beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null,
            basket: (basketId, 1));

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(8);
    }

    [Fact]
    public void RegisterWithdrawal_ComCestaQuandoBeneficiarioJaAtingiuOLimite_DeveBloquearEManterEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(maxPerCycle: 1);
        var clothingId = AddFreeItem(point);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(clothingId)], beneficiaryWithdrawalsInCurrentCycle: 1, TestData.Now, null,
            basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(100);
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComCestaAteOTeto_DevePermitir()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, _, basketId) = PointWithBasket(maxPerCycle: 3, riceQuantity: 100, quantityPerBasket: 1);
        var clothingId = AddFreeItem(point);

        point.RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 2, TestData.Now, null, basket: (basketId, 1))
            .IsError.Should().BeFalse();
        point.RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 3, TestData.Now, null, basket: (basketId, 1))
            .IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData("Roupas")]
    [InlineData("Calçados")]
    [InlineData("Higiene")]
    [InlineData("Brinquedos")]
    public void RegisterWithdrawal_SemCesta_NaoAplicaOLimiteDoCicloMesmoComACotaEsgotada(string itemName)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var itemId = TestData.AddStockItem(point, itemName, "un", 10);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(itemId)], beneficiaryWithdrawalsInCurrentCycle: 50, TestData.Now, null);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().Equal(new WithdrawalItem(itemId, 1));
        result.Value.CountsTowardsLimit.Should().BeFalse();
    }

    [Fact]
    public void RegisterWithdrawal_GuardaOItemDaRetirada()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);

        var result = point.RegisterWithdrawal(beneficiary, [TestData.Item(itemId, 2)], 0, TestData.Now, null);

        result.Value.Items.Should().Equal(new WithdrawalItem(itemId, 2));
        point.DomainEvents.OfType<WithdrawalRegistered>().Single().Items.Should().Equal(new WithdrawalItem(itemId, 2));
    }

    [Fact]
    public void RegisterWithdrawal_ComVariosItens_GuardaTodosEDescontaEstoqueDeCadaUm()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var foodId = TestData.AddStockItem(point, "Arroz", "kg", 10);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 5, TestData.AddCategory(point, "Roupas"));
        var footwearId = TestData.AddStockItem(point, "Sandália", "par", 3, TestData.AddCategory(point, "Calçados"));

        var result = point.RegisterWithdrawal(
            beneficiary,
            [TestData.Item(foodId, 2), TestData.Item(clothingId, 1), TestData.Item(footwearId, 1)],
            0,
            TestData.Now,
            null);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().BeEquivalentTo(
        [
            new WithdrawalItem(foodId, 2),
            new WithdrawalItem(clothingId, 1),
            new WithdrawalItem(footwearId, 1)
        ]);
        point.StockItems.Single(i => i.Id == foodId).Quantity.Should().Be(8);
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(4);
        point.StockItems.Single(i => i.Id == footwearId).Quantity.Should().Be(2);
    }

    [Fact]
    public void RegisterWithdrawal_SemNenhumItem_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(beneficiary, [], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.ItemsRequired");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComItemDuplicadoNaLista_DeveBloquearSemMutarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(itemId, 1), TestData.Item(itemId, 2)], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.DuplicateItem");
        point.StockItems.Single(i => i.Id == itemId).Quantity.Should().Be(10);
        point.Withdrawals.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RegisterWithdrawal_ComQuantidadeInvalida_DeveBloquearSemMutarEstoque(int invalidQuantity)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(itemId, invalidQuantity)], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.InvalidItemQuantity");
        point.StockItems.Single(i => i.Id == itemId).Quantity.Should().Be(10);
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComItemInexistenteNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(beneficiary, [TestData.Item(StockItemId.New())], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComEstoqueInsuficienteParaOItemLivre_DeveBloquearSemMutarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 3);

        var result = point.RegisterWithdrawal(beneficiary, [TestData.Item(itemId, 4)], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
        point.StockItems.Single(i => i.Id == itemId).Quantity.Should().Be(3);
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComEstoqueInsuficienteParaACesta_DeveBloquearSemMutarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(riceQuantity: 3, quantityPerBasket: 2);
        var clothingId = AddFreeItem(point);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null, basket: (basketId, 2));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(3);
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(100);
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComEstoqueExatamenteSuficienteParaACesta_DevePermitir()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket(riceQuantity: 4, quantityPerBasket: 2);
        var clothingId = AddFreeItem(point);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null, basket: (basketId, 2));

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(0);
    }

    [Fact]
    public void RegisterWithdrawal_ComReceitaInativa_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var (point, riceId, basketId) = PointWithBasket();
        var clothingId = AddFreeItem(point);
        point.UpdateBasketTemplate(basketId, "Cesta básica", [(riceId, 2)], isActive: false);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null, basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateInactive");
    }

    [Fact]
    public void RegisterWithdrawal_ComReceitaInexistente_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);

        var result = point.RegisterWithdrawal(
            beneficiary, [TestData.Item(itemId)], 0, TestData.Now, null, basket: (BasketTemplateId.New(), 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateNotFound");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoPontoInativo_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);
        point.Deactivate();

        var result = point.RegisterWithdrawal(beneficiary, [TestData.Item(itemId)], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.Inactive");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoSucesso_DeveDispararEvento()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);

        point.RegisterWithdrawal(beneficiary, [TestData.Item(itemId)], 0, TestData.Now, "Cesta básica");

        point.DomainEvents.Should().ContainSingle(e => e is WithdrawalRegistered);
    }

    [Fact]
    public void RegisterWithdrawal_UsaOLimiteDoPontoOndeARetiradaAcontece_NaoODeOutroPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var (strictPoint, _, strictBasketId) = PointWithBasket(maxPerCycle: 1);
        var (generousPoint, _, generousBasketId) = PointWithBasket(maxPerCycle: 3);
        var strictClothingId = AddFreeItem(strictPoint);
        var generousClothingId = AddFreeItem(generousPoint);

        var atStrict = strictPoint.RegisterWithdrawal(
            beneficiary, [TestData.Item(strictClothingId)], 1, TestData.Now, null, basket: (strictBasketId, 1));
        var atGenerous = generousPoint.RegisterWithdrawal(
            beneficiary, [TestData.Item(generousClothingId)], 1, TestData.Now, null, basket: (generousBasketId, 1));

        atStrict.IsError.Should().BeTrue();
        atGenerous.IsError.Should().BeFalse();
    }
}
