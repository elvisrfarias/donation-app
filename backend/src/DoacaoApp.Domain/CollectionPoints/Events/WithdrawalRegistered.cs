using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints.Events;

/// <summary>
/// Disparado quando uma retirada é registrada. É a base do histórico municipal
/// compartilhado: todos os pontos enxergam as retiradas de um beneficiário por este evento/registro.
/// </summary>
public sealed record WithdrawalRegistered(
    WithdrawalId WithdrawalId,
    CollectionPointId PointId,
    BeneficiaryId BeneficiaryId,
    IReadOnlyList<WithdrawalItem> Items,
    DateTime OccurredAtUtc) : IDomainEvent;
