namespace DoacaoApp.Domain.Families;

/// <summary>Identificador fortemente tipado da família (evita confundir Guids entre agregados).</summary>
public readonly record struct FamilyId(Guid Value)
{
    public static FamilyId New() => new(Guid.NewGuid());

    public static FamilyId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identificador fortemente tipado do beneficiário.</summary>
public readonly record struct BeneficiaryId(Guid Value)
{
    public static BeneficiaryId New() => new(Guid.NewGuid());

    public static BeneficiaryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
