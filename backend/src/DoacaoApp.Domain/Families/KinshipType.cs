namespace DoacaoApp.Domain.Families;

/// <summary>
/// Tipo de parentesco declarado entre dois beneficiários, no mesmo padrão usado em
/// cadastros sociais reais (ex.: CadÚnico). Todo vínculo é espelhado automaticamente no
/// outro beneficiário usando o tipo inverso (<see cref="KinshipTypeExtensions.Inverse"/>). Só
/// <see cref="Conjuge"/> entre beneficiários de famílias diferentes altera o agrupamento
/// familiar — os demais tipos são puramente informativos (não movem ninguém de família).
/// </summary>
public enum KinshipType
{
    /// <summary>Cônjuge ou companheiro(a) — o único tipo que forma uma nova família.</summary>
    Conjuge = 1,

    /// <summary>Pai ou mãe.</summary>
    Pai = 2,

    /// <summary>Filho(a) ou enteado(a).</summary>
    Filho = 3,

    /// <summary>Irmão ou irmã.</summary>
    Irmao = 4,

    /// <summary>Avô ou avó.</summary>
    Avo = 5,

    /// <summary>Neto(a) ou bisneto(a).</summary>
    Neto = 6,

    /// <summary>Tio ou tia.</summary>
    Tio = 7,

    /// <summary>Sobrinho(a).</summary>
    Sobrinho = 8,

    /// <summary>Primo(a).</summary>
    Primo = 9,

    /// <summary>Sogro(a).</summary>
    Sogro = 10,

    /// <summary>Genro ou nora.</summary>
    GenroOuNora = 11,

    /// <summary>Agregado(a): mora junto, sem parentesco sanguíneo ou por afinidade.</summary>
    Agregado = 12,

    /// <summary>Outro parentesco não listado.</summary>
    Outro = 13,
}

public static class KinshipTypeExtensions
{
    /// <summary>Tipo de parentesco visto do outro lado do vínculo (ex.: Pai ↔ Filho).</summary>
    public static KinshipType Inverse(this KinshipType type) => type switch
    {
        KinshipType.Conjuge => KinshipType.Conjuge,
        KinshipType.Pai => KinshipType.Filho,
        KinshipType.Filho => KinshipType.Pai,
        KinshipType.Irmao => KinshipType.Irmao,
        KinshipType.Avo => KinshipType.Neto,
        KinshipType.Neto => KinshipType.Avo,
        KinshipType.Tio => KinshipType.Sobrinho,
        KinshipType.Sobrinho => KinshipType.Tio,
        KinshipType.Primo => KinshipType.Primo,
        KinshipType.Sogro => KinshipType.GenroOuNora,
        KinshipType.GenroOuNora => KinshipType.Sogro,
        KinshipType.Agregado => KinshipType.Agregado,
        KinshipType.Outro => KinshipType.Outro,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de parentesco desconhecido."),
    };
}
