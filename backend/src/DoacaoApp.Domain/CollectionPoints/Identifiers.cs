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

/// <summary>Natureza jurídica do ponto de coleta.</summary>
public enum CollectionPointType
{
    Association = 1,
    Church = 2,
    PublicEntity = 3,
}

/// <summary>
/// Categorias de itens que um ponto de coleta recebe/disponibiliza (ex.: comida, roupas).
/// Um ponto pode declarar várias categorias.
/// </summary>
public enum ItemCategory
{
    /// <summary>Alimentos / comida.</summary>
    Food = 1,

    /// <summary>Roupas.</summary>
    Clothing = 2,

    /// <summary>Calçados.</summary>
    Footwear = 3,

    /// <summary>Produtos de higiene pessoal.</summary>
    Hygiene = 4,

    /// <summary>Produtos de limpeza.</summary>
    Cleaning = 5,

    /// <summary>Móveis.</summary>
    Furniture = 6,

    /// <summary>Eletrodomésticos.</summary>
    Appliances = 7,

    /// <summary>Brinquedos.</summary>
    Toys = 8,

    /// <summary>Livros e material escolar.</summary>
    Books = 9,

    /// <summary>Medicamentos.</summary>
    Medicine = 10,

    /// <summary>Outros itens não listados.</summary>
    Other = 99,
}