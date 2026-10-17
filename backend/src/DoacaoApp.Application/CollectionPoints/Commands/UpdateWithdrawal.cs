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
/// Corrige os itens/observações/cesta de uma retirada já registrada. Só a retirada mais recente do
/// beneficiário (considerando TODOS os pontos do município) pode ser editada — um ajuste tardio
/// poderia destoar do estoque e do histórico de ciclo já consolidados desde então. Como no registro, o
/// beneficiário precisa continuar ativo e vinculado ao ponto.
/// </summary>
public sealed record UpdateWithdrawalCommand(
    Guid PointId,
    Guid WithdrawalId,
    IReadOnlyList<WithdrawalItemEntry> Items,
    string? Notes,
    Guid? BasketTemplateId = null,
    int? BasketQuantity = null) : IRequest<ErrorOr<WithdrawalResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateWithdrawalValidator : AbstractValidator<UpdateWithdrawalCommand>
{
    public UpdateWithdrawalValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.WithdrawalId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.BasketQuantity)
            .GreaterThan(0)
            .When(x => x.BasketTemplateId is not null)
            .WithMessage("Informe a quantidade de cestas retiradas.");
    }
}

public sealed class UpdateWithdrawalHandler(
    ICollectionPointRepository pointRepository,
    IBeneficiaryRepository beneficiaryRepository,
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

        var beneficiary = await beneficiaryRepository.GetByIdAsync(existing.BeneficiaryId, cancellationToken);
        if (beneficiary is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var eligibility = WithdrawalEligibility.Check(beneficiary, point.Id);
        if (eligibility.IsError)
        {
            return eligibility.Errors;
        }

        var beneficiaryHistory = await pointRepository.GetBeneficiaryWithdrawalHistoryAsync(
            existing.BeneficiaryId, cancellationToken);
        var isMostRecentForBeneficiary = beneficiaryHistory.Count > 0 && beneficiaryHistory[0].Id == existing.Id;

        var (startUtc, endUtc) = point.WithdrawalRule.GetCurrentPeriod(existing.OccurredAtUtc);
        var totalInCycle = await pointRepository.CountBeneficiaryBasketWithdrawalsAsync(
            existing.BeneficiaryId, startUtc, endUtc, cancellationToken);
        var othersInCycle = existing.CountsTowardsLimit ? Math.Max(0, totalInCycle - 1) : totalInCycle;

        var basket = ToBasket(command.BasketTemplateId, command.BasketQuantity);

        var updated = point.UpdateWithdrawal(
            withdrawalId,
            isMostRecentForBeneficiary,
            RegisterWithdrawalHandler.ToDomainItems(command.Items),
            command.Notes,
            othersInCycle,
            basket);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.ToResponse(point.StockItems, point.Categories);
    }

    private static (BasketTemplateId Id, int Quantity)? ToBasket(Guid? basketTemplateId, int? basketQuantity) =>
        basketTemplateId is null ? null : (BasketTemplateId.From(basketTemplateId.Value), basketQuantity!.Value);
}
