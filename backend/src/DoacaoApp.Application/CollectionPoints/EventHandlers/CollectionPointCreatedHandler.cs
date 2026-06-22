using DoacaoApp.Domain.CollectionPoints.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.CollectionPoints.EventHandlers;

/// <summary>Auditoria da criação de um ponto de coleta.</summary>
public sealed class CollectionPointCreatedHandler(ILogger<CollectionPointCreatedHandler> logger)
    : INotificationHandler<CollectionPointCreated>
{
    public Task Handle(CollectionPointCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Ponto de coleta criado: {PointId} - {Name} ({Type})",
            notification.PointId.Value,
            notification.Name,
            notification.Type);

        return Task.CompletedTask;
    }
}