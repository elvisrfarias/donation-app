namespace DoacaoApp.Domain.Donations;

/// <summary>Identificador fortemente tipado da doação.</summary>
public readonly record struct DonationId(Guid Value)
{
    public static DonationId New() => new(Guid.NewGuid());

    public static DonationId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
