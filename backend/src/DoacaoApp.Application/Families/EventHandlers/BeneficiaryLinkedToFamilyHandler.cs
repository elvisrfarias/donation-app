using DoacaoApp.Domain.Families.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Families.EventHandlers;

/// <summary>Auditoria do vínculo beneficiário↔família.</summary>
public sealed class BeneficiaryLinkedToFamilyHandler(ILogger<BeneficiaryLinkedToFamilyHandler> logger)
    : INotificationHandler<BeneficiaryLinkedToFamily>
{
    public Task Handle(BeneficiaryLinkedToFamily notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Beneficiário {BeneficiaryId} vinculado à família {FamilyId} pelo ponto {PointId}",
            notification.BeneficiaryId.Value,
            notification.FamilyId.Value,
            notification.RegisteredByPointId.Value);

        return Task.CompletedTask;
    }
}