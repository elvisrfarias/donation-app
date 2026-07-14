namespace DoacaoApp.Domain.Reviews;

/// <summary>Identificador fortemente tipado da avaliação de um ponto de coleta.</summary>
public readonly record struct ReviewId(Guid Value)
{
    public static ReviewId New() => new(Guid.NewGuid());

    public static ReviewId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
