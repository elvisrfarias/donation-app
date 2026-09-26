using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointWithdrawalRuleTests
{
    [Fact]
    public void NovoPonto_TemARegraPadraoDeTresRetiradasPorAno()
    {
        var point = CollectionPoint.Create(
            "Paróquia São José", CollectionPointType.Church, TestData.Address(), TestData.Contact(), TestData.Now).Value;

        point.WithdrawalRule.Type.Should().Be(CycleType.Annual);
        point.WithdrawalRule.MaxWithdrawalsPerCycle.Should().Be(3);
    }

    [Fact]
    public void UpdateWithdrawalRule_DeveSubstituirARegraDoPonto()
    {
        var point = TestData.Point();
        var rule = TestData.Cycle(max: 3, type: CycleType.Biweekly);

        point.UpdateWithdrawalRule(rule);

        point.WithdrawalRule.Should().Be(rule);
    }
}
