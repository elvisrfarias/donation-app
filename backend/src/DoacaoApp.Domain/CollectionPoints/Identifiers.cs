namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>Identificador fortemente tipado do ponto de coleta.</summary>
public readonly record struct CollectionPointId(Guid Value)
{
    public static CollectionPointId New() => new(Guid.NewGuid());

    public static CollectionPointId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identificador fortemente tipado da retirada.</summary>
public readonly record struct WithdrawalId(Guid Value)
{
    public static WithdrawalId New() => new(Guid.NewGuid());

    public static WithdrawalId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identificador fortemente tipado do item de estoque granular de um ponto.</summary>
public readonly record struct StockItemId(Guid Value)
{
    public static StockItemId New() => new(Guid.NewGuid());

    public static StockItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identificador fortemente tipado da receita de cesta básica de um ponto.</summary>
public readonly record struct BasketTemplateId(Guid Value)
{
    public static BasketTemplateId New() => new(Guid.NewGuid());

    public static BasketTemplateId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identificador fortemente tipado da categoria de itens de um ponto.</summary>
public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());

    public static CategoryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Natureza jurídica do ponto de coleta.</summary>
public enum CollectionPointType
{
    Association = 1,
    Church = 2,
    PublicEntity = 3,
}