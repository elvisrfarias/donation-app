using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.ValueObjects;

public class WithdrawalCycleTests
{
    [Fact]
    public void Create_ComMaximoZeroOuNegativo_DeveRetornarErro()
    {
        WithdrawalCycle.Create(CycleType.Monthly, 0).IsError.Should().BeTrue();
        WithdrawalCycle.Create(CycleType.Monthly, -1).FirstError.Code.Should().Be("WithdrawalCycle.InvalidMax");
    }

    [Fact]
    public void Create_ComMaximoValido_DeveCriar()
    {
        var result = WithdrawalCycle.Create(CycleType.Monthly, 2);

        result.IsError.Should().BeFalse();
        result.Value.MaxWithdrawalsPerCycle.Should().Be(2);
    }

    [Fact]
    public void GetCurrentPeriod_Mensal_DeveRetornarPrimeiroAoUltimoDiaDoMes()
    {
        var cycle = WithdrawalCycle.Create(CycleType.Monthly, 1).Value;

        var (start, end) = cycle.GetCurrentPeriod(new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc));

        start.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        end.Should().Be(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetCurrentPeriod_Semanal_DeveComecarNaSegundaEDurarSeteDias()
    {
        var cycle = WithdrawalCycle.Create(CycleType.Weekly, 1).Value;
        var reference = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

        var (start, end) = cycle.GetCurrentPeriod(reference);

        start.DayOfWeek.Should().Be(DayOfWeek.Monday);
        end.Should().Be(start.AddDays(7));
        reference.Should().BeOnOrAfter(start).And.BeBefore(end);
    }

    [Fact]
    public void GetCurrentPeriod_Quinzenal_DeveDurarQuatorzeDias()
    {
        var cycle = WithdrawalCycle.Create(CycleType.Biweekly, 1).Value;
        var reference = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

        var (start, end) = cycle.GetCurrentPeriod(reference);

        (end - start).Days.Should().Be(14);
        reference.Should().BeOnOrAfter(start).And.BeBefore(end);
    }
}