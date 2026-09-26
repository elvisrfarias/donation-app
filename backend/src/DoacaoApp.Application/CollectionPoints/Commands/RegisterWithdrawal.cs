using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>
/// Registra uma retirada em um ponto de coleta aplicando a regra de equidade:
/// o beneficiário não pode exceder o limite do ponto no ciclo vigente, considerando o histórico
/// compartilhado de TODOS os pontos do município.
/// </summary>
public sealed record RegisterWithdrawalCommand(
    Guid PointId,
    Guid BeneficiaryId,
    DateTime? OccurredAtUtc,
    string? Notes) : IRequest<ErrorOr<WithdrawalResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class RegisterWithdrawalValidator : AbstractValidator<RegisterWithdrawalCommand>
{
    public RegisterWithdrawalValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RegisterWithdrawalHandler(
    ICollectionPointRepository pointRepository,
    IBeneficiaryRepository beneficiaryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<RegisterWithdrawalCommand, ErrorOr<WithdrawalResponse>>
{
    public async Task<ErrorOr<WithdrawalResponse>> Handle(
        RegisterWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        var occurredAt = (command.OccurredAtUtc ?? clock.UtcNow).ToUniversalTime();

        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            BeneficiaryId.From(command.BeneficiaryId), cancellationToken);
        if (beneficiary is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var (startUtc, endUtc) = point.WithdrawalRule.GetCurrentPeriod(occurredAt);

        var alreadyInCycle = await pointRepository.CountBeneficiaryWithdrawalsAsync(
            beneficiary.Id, startUtc, endUtc, cancellationToken);

        var withdrawalResult = point.RegisterWithdrawal(
            beneficiary, alreadyInCycle, occurredAt, command.Notes);

        if (withdrawalResult.IsError)
        {
            return withdrawalResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return withdrawalResult.Value.ToResponse();
    }
}