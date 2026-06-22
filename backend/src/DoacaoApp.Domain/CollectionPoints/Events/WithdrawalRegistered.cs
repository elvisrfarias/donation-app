using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;

namespace DoacaoApp.Domain.CollectionPoints.Events;

/// <summary>
/// Disparado quando uma retirada é registrada. É a base do histórico municipal
/// compartilhado: todos os pontos enxergam as retiradas de uma família por este evento/registro.
/// </summary>
public sealed record WithdrawalRegistered(
    WithdrawalId WithdrawalId,
    FamilyId FamilyId,
    CollectionPointId PointId,
    BeneficiaryId BeneficiaryId,
    DateTime OccurredAtUtc) : IDomainEvent;