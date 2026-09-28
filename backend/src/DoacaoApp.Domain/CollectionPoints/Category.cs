using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Categoria de itens — Entity DENTRO do agregado <see cref="CollectionPoint"/>. O catálogo de
/// categorias é próprio de cada ponto (não é compartilhado entre pontos do município). Serve três
/// papéis: o que o ponto declara aceitar/disponibilizar (mostrado no mapa público), o agrupamento
/// do catálogo de estoque (<see cref="StockItem"/>) e, indiretamente, dos itens livres de uma
/// retirada (<see cref="Withdrawal.Items"/> referencia o <see cref="StockItem"/>, que tem categoria).
/// </summary>
public sealed class Category : Entity<CategoryId>
{
    private Category()
    {
        Name = string.Empty;
        Icon = string.Empty;
    }

    private Category(CategoryId id, CollectionPointId pointId, string name, string icon) : base(id)
    {
        PointId = pointId;
        Name = name;
        Icon = icon;
    }

    public CollectionPointId PointId { get; private set; }

    /// <summary>Nome da categoria (ex.: "Alimentos"), único por ponto (sem diferenciar maiúsculas/minúsculas).</summary>
    public string Name { get; private set; }

    /// <summary>Chave do ícone escolhido na paleta do front — o backend só guarda, não valida contra uma lista fechada.</summary>
    public string Icon { get; private set; }

    internal static Category Create(CollectionPointId pointId, string name, string icon) =>
        new(CategoryId.New(), pointId, name.Trim(), icon.Trim());

    internal void UpdateDetails(string name, string icon)
    {
        Name = name.Trim();
        Icon = icon.Trim();
    }
}
