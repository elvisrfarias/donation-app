using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Domain.Tests;

/// <summary>Fábricas de objetos válidos para reduzir repetição nos testes.</summary>
public static class TestData
{
    public const string ValidCpfA = "111.444.777-35";
    public const string ValidCpfB = "529.982.247-25";

    public static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    public static Address Address() =>
        DoacaoApp.Domain.ValueObjects.Address.Create("Rua das Flores", "100", null, "Centro", "São Paulo", "SP", "01001000").Value;

    public static ContactInfo Contact() =>
        ContactInfo.Create("Maria Souza", "maria@doacao.org", "11987654321").Value;

    public static WithdrawalCycle Cycle(int max = 1, CycleType type = CycleType.Monthly) =>
        WithdrawalCycle.Create(type, max).Value;

    public static CollectionPoint Point(CollectionPointType type = CollectionPointType.Church, int maxPerCycle = 1)
    {
        var point = CollectionPoint.Create("Paróquia São José", type, Address(), Contact(), Now).Value;
        point.UpdateWithdrawalRule(Cycle(maxPerCycle));
        return point;
    }

    public static Beneficiary Beneficiary(string cpf = ValidCpfA, CollectionPointId? pointId = null) =>
        Domain.Beneficiaries.Beneficiary.Create(
            "João Silva",
            Cpf.Create(cpf).Value,
            Email.Create("joao@example.com").Value,
            [PhoneNumber.Create("11987654321").Value],
            null,
            new DateOnly(1990, 5, 20),
            pointId ?? CollectionPointId.New(),
            Now).Value;
}
