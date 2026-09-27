namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Resultado do cálculo de "quantas cestas completas dá para montar agora" com o estoque atual do
/// ponto, e o que falta para montar mais uma. Não é persistido — é um valor calculado sob demanda
/// a partir de <see cref="CollectionPoint.CalculateBasketFeasibility(BasketTemplateId)"/>.
/// </summary>
public sealed record BasketFeasibility(int CompleteBaskets, IReadOnlyList<BasketItemGap> Missing);

/// <summary>Situação de um item da receita: quanto precisa por cesta, quanto há, e quanto falta para a próxima cesta completa.</summary>
public sealed record BasketItemGap(
    StockItemId ItemId, string ItemName, int RequiredPerBasket, int CurrentQuantity, int NeededForNextBasket);
