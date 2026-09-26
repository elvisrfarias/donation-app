namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Configuração de estoque de uma categoria de item em um ponto: a meta desejada (0 = sem meta),
/// a quantidade a partir da qual o estoque é considerado baixo (aviso) e se a categoria é
/// acompanhada pelo ponto.
/// </summary>
public sealed record StockSetting(int Goal, int AlertThreshold, bool IsTracked);
