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

        var result = point.RegisterWithdrawal(beneficiary, beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null);

        result.IsError.Should().BeFalse();
        result.Value.BeneficiaryId.Should().Be(beneficiary.Id);
        point.Withdrawals.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterWithdrawal_QuandoBeneficiarioJaAtingiuOLimiteDoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);

        var result = point.RegisterWithdrawal(beneficiary, beneficiaryWithdrawalsInCurrentCycle: 1, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        point.Withdrawals.Should().BeEmpty();
    }

    [Fact]
    public void RegisterWithdrawal_ComMaiorLimite_DevePermitirAteOTeto()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);

        point.RegisterWithdrawal(beneficiary, 2, TestData.Now, null).IsError.Should().BeFalse();
        point.RegisterWithdrawal(beneficiary, 3, TestData.Now, null).IsError.Should().BeTrue();
    }

    [Fact]
    public void RegisterWithdrawal_QuandoPontoInativo_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.Deactivate();

        var result = point.RegisterWithdrawal(beneficiary, 0, TestData.Now, null);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.Inactive");
    }

    [Fact]
    public void RegisterWithdrawal_QuandoSucesso_DeveDispararEvento()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();

        point.RegisterWithdrawal(beneficiary, 0, TestData.Now, "Cesta básica");

        point.DomainEvents.Should().ContainSingle(e => e is WithdrawalRegistered);
    }

    [Fact]
    public void RegisterWithdrawal_UsaALimiteDoPontoOndeARetiradaAcontece_NaoODeOutroPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var strictPoint = TestData.Point(maxPerCycle: 1);
        var generousPoint = TestData.Point(maxPerCycle: 3);

        var atStrict = strictPoint.RegisterWithdrawal(beneficiary, 1, TestData.Now, null);
        var atGenerous = generousPoint.RegisterWithdrawal(beneficiary, 1, TestData.Now, null);

        atStrict.IsError.Should().BeTrue();
        atGenerous.IsError.Should().BeFalse();
    }
}
