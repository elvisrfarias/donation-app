using DoacaoApp.Domain.CollectionPoints.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.CollectionPoints.EventHandlers;

/// <summary>
/// Reage ao evento de domínio <see cref="WithdrawalRegistered"/>. Aqui ficaria a
/// notificação/integração; por ora, registra a retirada no log estruturado (auditoria).
/// </summary>
public sealed class WithdrawalRegisteredHandler(ILogger<WithdrawalRegisteredHandler> logger)
    : INotificationHandler<WithdrawalRegistered>
{
    public Task Handle(WithdrawalRegistered notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Retirada {WithdrawalId} registrada no ponto {PointId} para o beneficiário {BeneficiaryId} em {OccurredAt:u}",
            notification.WithdrawalId.Value,
            notification.PointId.Value,
            notification.BeneficiaryId.Value,
            notification.OccurredAtUtc);

        return Task.CompletedTask;
    }
}