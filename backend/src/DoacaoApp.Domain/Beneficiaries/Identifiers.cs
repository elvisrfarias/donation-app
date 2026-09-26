namespace DoacaoApp.Domain.Beneficiaries;

/// <summary>Identificador fortemente tipado do beneficiário.</summary>
public readonly record struct BeneficiaryId(Guid Value)
{
    public static BeneficiaryId New() => new(Guid.NewGuid());

    public static BeneficiaryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
