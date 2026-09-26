using DoacaoApp.Domain.Beneficiaries.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Beneficiaries.EventHandlers;

/// <summary>Auditoria do cadastro de beneficiário.</summary>
public sealed class BeneficiaryRegisteredHandler(ILogger<BeneficiaryRegisteredHandler> logger)
    : INotificationHandler<BeneficiaryRegistered>
{
    public Task Handle(BeneficiaryRegistered notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Beneficiário {BeneficiaryId} cadastrado pelo ponto {PointId}",
            notification.BeneficiaryId.Value,
            notification.RegisteredByPointId.Value);

        return Task.CompletedTask;
    }
}
