namespace DoacaoApp.Domain.Sponsors;

/// <summary>Identificador fortemente tipado do patrocinador.</summary>
public readonly record struct SponsorId(Guid Value)
{
    public static SponsorId New() => new(Guid.NewGuid());

    public static SponsorId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
