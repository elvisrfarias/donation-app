using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>Histórico de retiradas de uma família (todos os pontos) + saldo do ciclo vigente.</summary>
public sealed record GetFamilyWithdrawalHistoryQuery(Guid FamilyId)
    : IRequest<ErrorOr<FamilyWithdrawalHistoryResponse>>;

public sealed class GetFamilyWithdrawalHistoryHandler(
    IFamilyRepository familyRepository,
    ICollectionPointRepository pointRepository,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
    : IRequestHandler<GetFamilyWithdrawalHistoryQuery, ErrorOr<FamilyWithdrawalHistoryResponse>>
{
    public async Task<ErrorOr<FamilyWithdrawalHistoryResponse>> Handle(
        GetFamilyWithdrawalHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var family = await familyRepository.GetByIdAsync(FamilyId.From(query.FamilyId), cancellationToken);
        if (family is null || !currentUser.CanAccess(family))
        {
            return DomainErrors.Family.NotFound;
        }

        var (startUtc, endUtc) = family.Cycle.GetCurrentPeriod(clock.UtcNow);

        var usedInCycle = await pointRepository.CountFamilyWithdrawalsAsync(
            family.Id, startUtc, endUtc, cancellationToken);

        var history = await pointRepository.GetFamilyWithdrawalHistoryAsync(family.Id, cancellationToken);

        var remaining = Math.Max(0, family.Cycle.MaxWithdrawalsPerCycle - usedInCycle);

        return new FamilyWithdrawalHistoryResponse(
            family.Id.Value,
            family.Name,
            family.Cycle.Type.ToString(),
            family.Cycle.MaxWithdrawalsPerCycle,
            usedInCycle,
            remaining,
            startUtc,
            endUtc,
            history.Select(w => w.ToResponse()).ToList());
    }
}