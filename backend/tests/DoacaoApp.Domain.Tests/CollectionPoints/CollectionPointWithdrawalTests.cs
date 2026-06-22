using DoacaoApp.Domain.CollectionPoints.Events;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointWithdrawalTests
{
    [Fact]
    public void RegisterWithdrawal_DentroDoLimite_DeveRegistrar()
    {
        var family = TestData.Family(maxPerCycle: 1);
        var beneficiary = TestData.AddBeneficiary(family);
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(family, beneficiary, familyWithdrawalsInCurrentCycle: 0, TestData.Now, null);

        result.IsError.Should().BeFalse();
        point.Withdrawals.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterWithdrawal_QuandoFamiliaJaAtingiuOLimite_DeveBloquear()
    {
        var family = TestData.Family(maxPerCycle: 1);
        var beneficiary = TestData.AddBeneficiary(family);
        var point = TestData.Point();

        // Já existe 1 retirada no ciclo (em qualquer ponto do município).
        var result = point.RegisterWithdrawal(family, beneficiary, familyWithdrawalsInCurrentCycle: 1, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComMaiorLimite_DevePermitirAteOTeto()
    {
        var family = TestData.Family(maxPerCycle: 3);
        var beneficiary = TestData.AddBeneficiary(family);
        var point = TestData.Point();

        point.RegisterWithdrawal(family, beneficiary, 2, TestData.Now, null).IsError.Should().BeFalse();
        point.RegisterWithdrawal(family, beneficiary, 3, TestData.Now, null).IsError.Should().BeTrue();
    }

    [Fact]
    public void RegisterWithdrawal_BeneficiarioDeOutraFamilia_DeveBloquear()
    {
        var family = TestData.Family();
        var otherFamily = TestData.Family();
        var beneficiaryFromOther = TestData.AddBeneficiary(otherFamily, TestData.ValidCpfB);
        var point = TestData.Point();

        var result = point.RegisterWithdrawal(family, beneficiaryFromOther, 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.BeneficiaryNotInFamily");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoPontoInativo_DeveBloquear()
    {
        var family = TestData.Family();
        var beneficiary = TestData.AddBeneficiary(family);
        var point = TestData.Point();
        point.Deactivate();

        var result = point.RegisterWithdrawal(family, beneficiary, 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.Inactive");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoSucesso_DeveDispararEvento()
    {
        var family = TestData.Family();
        var beneficiary = TestData.AddBeneficiary(family);
        var point = TestData.Point();

        point.RegisterWithdrawal(family, beneficiary, 0, TestData.Now, "Cesta básica");

        point.DomainEvents.Should().ContainSingle(e => e is WithdrawalRegistered);
    }
}