using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Domain.Tests;

/// <summary>Fábricas de objetos válidos para reduzir repetição nos testes.</summary>
public static class TestData
{
    // CPFs válidos (dígitos verificadores corretos).
    public const string ValidCpfA = "111.444.777-35";
    public const string ValidCpfB = "529.982.247-25";

    public static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    public static Address Address() =>
        DoacaoApp.Domain.ValueObjects.Address.Create("Rua das Flores", "100", null, "Centro", "São Paulo", "SP", "01001000").Value;

    public static ContactInfo Contact() =>
        ContactInfo.Create("Maria Souza", "maria@doacao.org", "11987654321").Value;

    public static WithdrawalCycle Cycle(int max = 1, CycleType type = CycleType.Monthly) =>
        WithdrawalCycle.Create(type, max).Value;

    public static CollectionPoint Point(CollectionPointType type = CollectionPointType.Church) =>
        CollectionPoint.Create("Paróquia São José", type, Address(), Contact(), Now).Value;

    public static Family Family(int maxPerCycle = 1) =>
        Domain.Families.Family.Create("Família Silva", Cycle(maxPerCycle), Now).Value;

    public static Beneficiary AddBeneficiary(Family family, string cpf = ValidCpfA, CollectionPointId? pointId = null) =>
        family.AddBeneficiary("João Silva", Cpf.Create(cpf).Value, null, pointId ?? CollectionPointId.New(), Now).Value;
}