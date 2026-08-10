using DoacaoApp.Domain.Storage.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Storage.EventHandlers;

/// <summary>Auditoria da remoção lógica de um arquivo.</summary>
public sealed class StoredFileDeletedHandler(ILogger<StoredFileDeletedHandler> logger)
    : INotificationHandler<StoredFileDeleted>
{
    public Task Handle(StoredFileDeleted notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Arquivo removido: {FileId} - {Key}",
            notification.FileId.Value,
            notification.ObjectKey);

        return Task.CompletedTask;
    }
}
