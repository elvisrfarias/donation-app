using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Application.Tests.Common;

/// <summary>Fábricas de objetos válidos para reduzir repetição nos testes de Application.</summary>
public static class TestData
{
    public const string ValidCpfA = "111.444.777-35";
    public const string ValidCpfB = "529.982.247-25";

    public static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    public static Beneficiary Beneficiary(
        string name = "João",
        string cpf = ValidCpfA,
        CollectionPointId? pointId = null) =>
        Domain.Beneficiaries.Beneficiary.Create(
            name,
            Cpf.Create(cpf).Value,
            Email.Create("joao@example.com").Value,
            [PhoneNumber.Create("11987654321").Value],
            null,
            new DateOnly(1990, 5, 20),
            pointId ?? CollectionPointId.New(),
            Now).Value;

    public static CollectionPoint Point(int maxPerCycle = 1)
    {
        var address = Address.Create("Rua A", "1", null, "Centro", "São Paulo", "SP", "01001000").Value;
        var contact = ContactInfo.Create("Maria", "maria@x.com", "11987654321").Value;
        var point = CollectionPoint.Create("Ponto", CollectionPointType.Church, address, contact, Now).Value;
        point.UpdateWithdrawalRule(WithdrawalCycle.Create(CycleType.Monthly, maxPerCycle).Value);
        return point;
    }
}
