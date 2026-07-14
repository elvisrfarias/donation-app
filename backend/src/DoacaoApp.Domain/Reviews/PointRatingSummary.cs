using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Domain.Reviews;

/// <summary>
/// Resumo agregado das avaliações de um ponto de coleta (média e total), usado para
/// enriquecer a listagem pública e ordenar os pontos mais bem avaliados.
/// </summary>
public sealed record PointRatingSummary(CollectionPointId PointId, double AverageRating, int ReviewCount);
