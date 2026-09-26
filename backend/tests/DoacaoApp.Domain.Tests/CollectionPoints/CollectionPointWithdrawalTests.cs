using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.CollectionPoints.Events;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointWithdrawalTests
{
    [Fact]
    public void RegisterWithdrawal_DentroDoLimite_DeveRegistrar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null);

        result.IsError.Should().BeFalse();
        result.Value.BeneficiaryId.Should().Be(beneficiary.Id);
        point.Withdrawals.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterWithdrawal_QuandoBeneficiarioJaAtingiuOLimiteDoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], beneficiaryWithdrawalsInCurrentCycle: 1, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComMaiorLimite_DevePermitirAteOTeto()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);

        point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 2, TestData.Now, null).IsError.Should().BeFalse();
        point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 3, TestData.Now, null).IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(ItemCategory.Clothing)]
    [InlineData(ItemCategory.Footwear)]
    [InlineData(ItemCategory.Hygiene)]
    [InlineData(ItemCategory.Toys)]
    public void RegisterWithdrawal_DeCategoriaSemLimite_NaoAplicaOLimiteDoCiclo(ItemCategory category)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, [category], 50, TestData.Now, null);

        result.IsError.Should().BeFalse();
        result.Value.Categories.Should().Equal(category);
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
    public void RegisterWithdrawal_ComCestaJuntoDeOutrosItens_AplicaOLimiteDoCiclo()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Clothing, ItemCategory.Food], 1, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
    }

    [Fact]
    public void RegisterWithdrawal_SoComItensSemLimite_PermiteMesmoComACotaDeCestasEsgotada()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Clothing, ItemCategory.Footwear], 9, TestData.Now, null);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void RegisterWithdrawal_ComItemSemEstoqueNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.ReplaceStock(new Dictionary<ItemCategory, int> { [ItemCategory.Food] = 5 });

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.ItemUnavailable");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComItemZeradoNoEstoque_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.ReplaceStock(new Dictionary<ItemCategory, int> { [ItemCategory.Food] = 0, [ItemCategory.Clothing] = 5 });

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null);

        result.FirstError.Code.Should().Be("Withdrawal.ItemUnavailable");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoUmDosItensNaoTemEstoque_BloqueiaATodos()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.ReplaceStock(new Dictionary<ItemCategory, int> { [ItemCategory.Food] = 5 });

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food, ItemCategory.Clothing], 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.ItemUnavailable");
    }

    [Fact]
    public void RegisterWithdrawal_ComEstoqueSuficienteParaTodosOsItens_DevePermitir()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.ReplaceStock(new Dictionary<ItemCategory, int> { [ItemCategory.Food] = 5, [ItemCategory.Clothing] = 2 });

        var result = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food, ItemCategory.Clothing], 0, TestData.Now, null);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ApenasCestasBasicasContamParaOLimiteDoCiclo()
    {
        ItemCategory.Food.CountsTowardsWithdrawalLimit().Should().BeTrue();
        Enum.GetValues<ItemCategory>()
            .Where(category => category != ItemCategory.Food)
            .Should().OnlyContain(category => !category.CountsTowardsWithdrawalLimit());
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
    public void RegisterWithdrawal_UsaALimiteDoPontoOndeARetiradaAcontece_NaoODeOutroPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var strictPoint = TestData.Point(maxPerCycle: 1);
        var generousPoint = TestData.Point(maxPerCycle: 3);

        var atStrict = strictPoint.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 1, TestData.Now, null);
        var atGenerous = generousPoint.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 1, TestData.Now, null);

        atStrict.IsError.Should().BeTrue();
        atGenerous.IsError.Should().BeFalse();
    }
}
