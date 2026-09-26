using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.Beneficiaries.Events;

/// <summary>
/// Disparado quando um beneficiário é cadastrado. Registra qual ponto fez o cadastro
/// (rastreabilidade).
/// </summary>
public sealed record BeneficiaryRegistered(
    BeneficiaryId BeneficiaryId,
    CollectionPointId RegisteredByPointId) : IDomainEvent;
