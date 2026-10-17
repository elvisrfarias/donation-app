using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Histórico de retiradas de um beneficiário (todos os pontos) + saldo do ciclo vigente segundo a
/// regra do ponto consultado. Escopada ao ponto: um gestor consulta apenas pelo próprio ponto, que é
/// onde a retirada vai acontecer, e só de beneficiários vinculados a ele; o histórico em si é
/// compartilhado entre todos os pontos.
/// </summary>
public sealed record GetBeneficiaryWithdrawalHistoryQuery(Guid BeneficiaryId, Guid PointId)
    : IRequest<ErrorOr<BeneficiaryWithdrawalHistoryResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class GetBeneficiaryWithdrawalHistoryHandler(
    IBeneficiaryRepository beneficiaryRepository,
    ICollectionPointRepository pointRepository,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
    : IRequestHandler<GetBeneficiaryWithdrawalHistoryQuery, ErrorOr<BeneficiaryWithdrawalHistoryResponse>>
{
    /// <summary>
    /// Valor fixo de <see cref="BeneficiaryWithdrawalHistoryResponse.LimitedCategory"/>: o limite do
    /// ciclo não é amarrado a uma categoria específica — é "levar cesta básica ou não"
    /// (<see cref="Withdrawal.CountsTowardsLimit"/>).
    /// </summary>
    private const string LimitedCategoryLabel = "Basket";

    public async Task<ErrorOr<BeneficiaryWithdrawalHistoryResponse>> Handle(
        GetBeneficiaryWithdrawalHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            BeneficiaryId.From(query.BeneficiaryId), cancellationToken);
        if (beneficiary is null || !currentUser.CanAccess(beneficiary))
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(query.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var rule = point.WithdrawalRule;
        var (startUtc, endUtc) = rule.GetCurrentPeriod(clock.UtcNow);

        var usedInCycle = await pointRepository.CountBeneficiaryBasketWithdrawalsAsync(
            beneficiary.Id, startUtc, endUtc, cancellationToken);

        var history = await pointRepository.GetBeneficiaryWithdrawalHistoryAsync(beneficiary.Id, cancellationToken);

        var stockItemIds = history.SelectMany(w => w.Items).Select(item => item.StockItemId).Distinct().ToList();
        var stockItems = await pointRepository.GetStockItemsByIdsAsync(stockItemIds, cancellationToken);
        var categoryIds = stockItems.Select(item => item.CategoryId).Distinct().ToList();
        var categories = await pointRepository.GetCategoriesByIdsAsync(categoryIds, cancellationToken);

        var remaining = Math.Max(0, rule.MaxWithdrawalsPerCycle - usedInCycle);

        return new BeneficiaryWithdrawalHistoryResponse(
            beneficiary.Id.Value,
            beneficiary.Name,
            LimitedCategoryLabel,
            rule.Type.ToString(),
            rule.MaxWithdrawalsPerCycle,
            usedInCycle,
            remaining,
            startUtc,
            endUtc,
            history.Select(w => w.ToResponse(stockItems, categories)).ToList());
    }
}
