using DoacaoApp.Domain.Storage.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Storage.EventHandlers;

/// <summary>Auditoria do upload de um arquivo.</summary>
public sealed class StoredFileUploadedHandler(ILogger<StoredFileUploadedHandler> logger)
    : INotificationHandler<StoredFileUploaded>
{
    public Task Handle(StoredFileUploaded notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Arquivo armazenado: {FileId} - {Key} ({ContentType}, {SizeBytes} bytes)",
            notification.FileId.Value,
            notification.ObjectKey,
            notification.ContentType,
            notification.SizeBytes);

        return Task.CompletedTask;
    }
}
