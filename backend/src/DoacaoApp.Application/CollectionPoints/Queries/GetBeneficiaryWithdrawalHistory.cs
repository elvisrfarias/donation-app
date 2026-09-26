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
/// onde a retirada vai acontecer; o histórico em si é compartilhado entre todos os pontos.
/// </summary>
public sealed record GetBeneficiaryWithdrawalHistoryQuery(Guid BeneficiaryId, Guid PointId)
    : IRequest<ErrorOr<BeneficiaryWithdrawalHistoryResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class GetBeneficiaryWithdrawalHistoryHandler(
    IBeneficiaryRepository beneficiaryRepository,
    ICollectionPointRepository pointRepository,
    IDateTimeProvider clock)
    : IRequestHandler<GetBeneficiaryWithdrawalHistoryQuery, ErrorOr<BeneficiaryWithdrawalHistoryResponse>>
{
    public async Task<ErrorOr<BeneficiaryWithdrawalHistoryResponse>> Handle(
        GetBeneficiaryWithdrawalHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            BeneficiaryId.From(query.BeneficiaryId), cancellationToken);
        if (beneficiary is null)
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

        var usedInCycle = await pointRepository.CountBeneficiaryWithdrawalsAsync(
            beneficiary.Id, ItemCategoryExtensions.WithdrawalLimitedCategory, startUtc, endUtc, cancellationToken);

        var history = await pointRepository.GetBeneficiaryWithdrawalHistoryAsync(beneficiary.Id, cancellationToken);

        var remaining = Math.Max(0, rule.MaxWithdrawalsPerCycle - usedInCycle);

        return new BeneficiaryWithdrawalHistoryResponse(
            beneficiary.Id.Value,
            beneficiary.Name,
            ItemCategoryExtensions.WithdrawalLimitedCategory.ToString(),
            rule.Type.ToString(),
            rule.MaxWithdrawalsPerCycle,
            usedInCycle,
            remaining,
            startUtc,
            endUtc,
            history.Select(w => w.ToResponse()).ToList());
    }
}
