namespace DoacaoApp.Domain.Notices;

/// <summary>Identificador fortemente tipado do aviso do mural.</summary>
public readonly record struct NoticeId(Guid Value)
{
    public static NoticeId New() => new(Guid.NewGuid());

    public static NoticeId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
