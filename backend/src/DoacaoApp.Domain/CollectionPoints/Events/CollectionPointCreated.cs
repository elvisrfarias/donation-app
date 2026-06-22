using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints.Events;

/// <summary>Disparado quando um novo ponto de coleta é cadastrado.</summary>
public sealed record CollectionPointCreated(
    CollectionPointId PointId,
    string Name,
    CollectionPointType Type) : IDomainEvent;