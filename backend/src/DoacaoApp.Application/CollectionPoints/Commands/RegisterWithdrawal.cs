using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>
/// Registra uma retirada em um ponto de coleta aplicando a regra de equidade:
/// a família não pode exceder seu limite no ciclo vigente, considerando o histórico
/// compartilhado de TODOS os pontos do município.
/// </summary>
public sealed record RegisterWithdrawalCommand(
    Guid PointId,
    Guid FamilyId,
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
        RuleFor(x => x.FamilyId).NotEmpty();
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RegisterWithdrawalHandler(
    ICollectionPointRepository pointRepository,
    IFamilyRepository familyRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<RegisterWithdrawalCommand, ErrorOr<WithdrawalResponse>>
{
    public async Task<ErrorOr<WithdrawalResponse>> Handle(
        RegisterWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        var occurredAt = (command.OccurredAtUtc ?? clock.UtcNow).ToUniversalTime();

        var family = await familyRepository.GetByIdAsync(FamilyId.From(command.FamilyId), cancellationToken);
        if (family is null)
        {
            return DomainErrors.Family.NotFound;
        }

        var beneficiary = family.FindBeneficiary(BeneficiaryId.From(command.BeneficiaryId));
        if (beneficiary is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var (startUtc, endUtc) = family.Cycle.GetCurrentPeriod(occurredAt);

        var alreadyInCycle = await pointRepository.CountFamilyWithdrawalsAsync(
            family.Id, startUtc, endUtc, cancellationToken);

        var withdrawalResult = point.RegisterWithdrawal(
            family, beneficiary, alreadyInCycle, occurredAt, command.Notes);

        if (withdrawalResult.IsError)
        {
            return withdrawalResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return withdrawalResult.Value.ToResponse();
    }
}