using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Domain.Beneficiaries;

/// <summary>
/// Vínculo entre um beneficiário e um ponto de coleta que o atende. Uma mesma pessoa (CPF único no
/// município) pode estar vinculada a vários pontos; cada ponto só enxerga e edita quem está vinculado a ele.
/// </summary>
public sealed record BeneficiaryPointLink(CollectionPointId PointId, DateTime LinkedAtUtc);
