using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.Families.Events;

/// <summary>
/// Disparado quando um beneficiário é cadastrado e vinculado a uma família.
/// Registra qual ponto fez o cadastro (rastreabilidade).
/// </summary>
public sealed record BeneficiaryLinkedToFamily(
    BeneficiaryId BeneficiaryId,
    FamilyId FamilyId,
    CollectionPointId RegisteredByPointId) : IDomainEvent;