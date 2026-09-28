using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointWithdrawalEditTests
{
    [Fact]
    public void UpdateWithdrawal_ComDadosValidos_DeveAlterarItensENotas()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var footwearId = TestData.AddStockItem(point, "Sandália", "par", 5, TestData.AddCategory(point, "Calçados"));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(clothingId, 2)], 0, TestData.Now, "Nota original")
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, isMostRecentForBeneficiary: true, [TestData.Item(footwearId, 1)], "Nota nova", 0);

        result.IsError.Should().BeFalse();
        withdrawal.Items.Should().Equal(new WithdrawalItem(footwearId, 1));
        withdrawal.Notes.Should().Be("Nota nova");
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(10);
        point.StockItems.Single(i => i.Id == footwearId).Quantity.Should().Be(4);
    }

    [Fact]
    public void UpdateWithdrawal_RetiradaInexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);

        var result = point.UpdateWithdrawal(
            WithdrawalId.New(), isMostRecentForBeneficiary: true, [TestData.Item(itemId)], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.NotFound");
    }

    [Fact]
    public void UpdateWithdrawal_QuandoNaoEAMaisRecenteDoBeneficiario_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var footwearId = TestData.AddStockItem(point, "Sandália", "par", 5, TestData.AddCategory(point, "Calçados"));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, isMostRecentForBeneficiary: false, [TestData.Item(footwearId)], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.OnlyMostRecentIsEditable");
        withdrawal.Items.Should().Equal(new WithdrawalItem(clothingId, 1));
    }

    [Fact]
    public void UpdateWithdrawal_SemNenhumItem_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.ItemsRequired");
        withdrawal.Items.Should().Equal(new WithdrawalItem(clothingId, 1));
    }

    [Fact]
    public void UpdateWithdrawal_ComItemInexistenteNoPonto_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [TestData.Item(StockItemId.New())], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
        withdrawal.Items.Should().Equal(new WithdrawalItem(clothingId, 1));
    }

    [Fact]
    public void UpdateWithdrawal_ComItemDuplicadoNaLista_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 10);
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(itemId, 1)], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [TestData.Item(itemId, 1), TestData.Item(itemId, 2)], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.DuplicateItem");
        withdrawal.Items.Should().Equal(new WithdrawalItem(itemId, 1));
    }

    [Fact]
    public void UpdateWithdrawal_ComEstoqueInsuficienteParaOItemLivre_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 5);
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(itemId, 3)], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [TestData.Item(itemId, 6)], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
        withdrawal.Items.Should().Equal(new WithdrawalItem(itemId, 3));
        point.StockItems.Single(i => i.Id == itemId).Quantity.Should().Be(2);
    }

    [Fact]
    public void UpdateWithdrawal_AumentandoQuantidadeDoMesmoItem_ConsideraAReservaAntigaAoValidarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var itemId = TestData.AddStockItem(point, "Camiseta", "un", 5);
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(itemId, 3)], 0, TestData.Now, null)
            .Value;
        point.StockItems.Single(i => i.Id == itemId).Quantity.Should().Be(2);

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [TestData.Item(itemId, 5)], null, 0);

        result.IsError.Should().BeFalse();
        withdrawal.Items.Should().Equal(new WithdrawalItem(itemId, 5));
        point.StockItems.Single(i => i.Id == itemId).Quantity.Should().Be(0);
    }

    [Fact]
    public void UpdateWithdrawal_ComCestaUltrapassandoOLimiteDoCiclo_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var foodCategoryId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, "Arroz", "kg", 10, foodCategoryId);
        var beansId = TestData.AddStockItem(point, "Feijão", "kg", 10, foodCategoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 2));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [TestData.Item(beansId)], null, beneficiaryWithdrawalsInCurrentCycleExcludingThis: 1,
            basket: (basketId, 1));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        withdrawal.Items.Should().Equal(new WithdrawalItem(clothingId, 1));
        withdrawal.BasketTemplateId.Should().BeNull();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(9);
        point.StockItems.Single(i => i.Id == beansId).Quantity.Should().Be(10);
    }

    [Fact]
    public void UpdateWithdrawal_MantendoCestaJaContabilizadaNelaMesma_NaoDeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var foodCategoryId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, "Arroz", "kg", 10, foodCategoryId);
        var beansId = TestData.AddStockItem(point, "Feijão", "kg", 10, foodCategoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 2));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(beansId)], 0, TestData.Now, null, basket: (basketId, 1))
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [TestData.Item(beansId), TestData.Item(clothingId)], "Ajustado",
            beneficiaryWithdrawalsInCurrentCycleExcludingThis: 0, basket: (basketId, 1));

        result.IsError.Should().BeFalse();
        withdrawal.Items.Should().Equal(new WithdrawalItem(beansId, 1), new WithdrawalItem(clothingId, 1));
        withdrawal.Notes.Should().Be("Ajustado");
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(8);
        point.StockItems.Single(i => i.Id == beansId).Quantity.Should().Be(9);
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(9);
    }

    [Fact]
    public void UpdateWithdrawal_TrocandoDeCesta_DevolveOEstoqueDaAntigaAntesDeValidarANova()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        var foodCategoryId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, "Arroz", "kg", 10, foodCategoryId);
        var beansId = TestData.AddStockItem(point, "Feijão", "kg", 10, foodCategoryId);
        var smallBasketId = TestData.CreateBasketTemplate(point, "Cesta pequena", (riceId, 8));
        var bigBasketId = TestData.CreateBasketTemplate(point, "Cesta grande", (riceId, 10));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(beansId)], 0, TestData.Now, null, basket: (smallBasketId, 1))
            .Value;
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(2);

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [TestData.Item(beansId)], null,
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
        var foodCategoryId = TestData.AddCategory(point, "Alimentos");
        var riceId = TestData.AddStockItem(point, "Arroz", "kg", 10, foodCategoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, TestData.AddCategory(point, "Roupas"));
        var basketId = TestData.CreateBasketTemplate(point, "Cesta básica", (riceId, 3));
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [TestData.Item(clothingId)], 0, TestData.Now, null, basket: (basketId, 1))
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [TestData.Item(clothingId)], null, 0);

        result.IsError.Should().BeFalse();
        withdrawal.BasketTemplateId.Should().BeNull();
        point.StockItems.Single(i => i.Id == riceId).Quantity.Should().Be(10);
    }
}
