using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>
/// Corrige os itens/observações de uma retirada já registrada. Só a retirada mais recente do
/// beneficiário (considerando TODOS os pontos do município) pode ser editada — um ajuste tardio
/// poderia destoar do estoque e do histórico de ciclo já consolidados desde então.
/// </summary>
public sealed record UpdateWithdrawalCommand(
    Guid PointId,
    Guid WithdrawalId,
    IReadOnlyList<ItemCategory> Categories,
    string? Notes) : IRequest<ErrorOr<WithdrawalResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateWithdrawalValidator : AbstractValidator<UpdateWithdrawalCommand>
{
    public UpdateWithdrawalValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.WithdrawalId).NotEmpty();
        RuleForEach(x => x.Categories).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class UpdateWithdrawalHandler(
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWithdrawalCommand, ErrorOr<WithdrawalResponse>>
{
    public async Task<ErrorOr<WithdrawalResponse>> Handle(
        UpdateWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var withdrawalId = WithdrawalId.From(command.WithdrawalId);
        var existing = point.Withdrawals.FirstOrDefault(w => w.Id == withdrawalId);
        if (existing is null)
        {
            return DomainErrors.Withdrawal.NotFound;
        }

        var beneficiaryHistory = await pointRepository.GetBeneficiaryWithdrawalHistoryAsync(
            existing.BeneficiaryId, cancellationToken);
        var isMostRecentForBeneficiary = beneficiaryHistory.Count > 0 && beneficiaryHistory[0].Id == existing.Id;

        var (startUtc, endUtc) = point.WithdrawalRule.GetCurrentPeriod(existing.OccurredAtUtc);
        var totalInCycle = await pointRepository.CountBeneficiaryWithdrawalsAsync(
            existing.BeneficiaryId, ItemCategoryExtensions.WithdrawalLimitedCategory, startUtc, endUtc, cancellationToken);
        var othersInCycle = existing.CountsTowardsLimit ? Math.Max(0, totalInCycle - 1) : totalInCycle;

        var updated = point.UpdateWithdrawal(
            withdrawalId, isMostRecentForBeneficiary, command.Categories, command.Notes, othersInCycle);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.ToResponse();
    }
}
