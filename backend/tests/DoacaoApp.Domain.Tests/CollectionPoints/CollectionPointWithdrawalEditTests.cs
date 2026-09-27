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
    public void UpdateWithdrawal_ComItemSemEstoque_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.ReplaceStock(new Dictionary<ItemCategory, int> { [ItemCategory.Clothing] = 5 });
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [ItemCategory.Footwear], null, 0);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.ItemUnavailable");
        withdrawal.Categories.Should().Equal(ItemCategory.Clothing);
    }

    [Fact]
    public void UpdateWithdrawal_QuandoUltrapassaOLimiteDoCiclo_DeveBloquearEManterAOriginal()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(withdrawal.Id, true, [ItemCategory.Food], null, 1);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        withdrawal.Categories.Should().Equal(ItemCategory.Clothing);
    }

    [Fact]
    public void UpdateWithdrawal_MantendoCestaJaContabilizadaNelaMesma_NaoDeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var withdrawal = point
            .RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now, null)
            .Value;

        var result = point.UpdateWithdrawal(
            withdrawal.Id, true, [ItemCategory.Food, ItemCategory.Clothing], "Ajustado", 0);

        result.IsError.Should().BeFalse();
        withdrawal.Categories.Should().Equal(ItemCategory.Food, ItemCategory.Clothing);
        withdrawal.Notes.Should().Be("Ajustado");
    }
}
