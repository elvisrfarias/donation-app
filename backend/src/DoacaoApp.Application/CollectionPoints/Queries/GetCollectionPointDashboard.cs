using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Indicadores de resultado de um ponto em um período: retiradas por dia (ou por mês em períodos
/// longos), atendimentos e últimas retiradas. Sem período, usa o mês atual completo. Escopada ao
/// ponto — um gestor só enxerga o painel do próprio ponto.
/// </summary>
public sealed record GetCollectionPointDashboardQuery(Guid PointId, DateOnly? From, DateOnly? To)
    : IRequest<ErrorOr<CollectionPointDashboardResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class GetCollectionPointDashboardValidator : AbstractValidator<GetCollectionPointDashboardQuery>
{
    public const int MaxPeriodInDays = 366;

    public GetCollectionPointDashboardValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.From!.Value <= x.To!.Value)
            .WithMessage("A data inicial não pode ser maior que a data final.")
            .Must(x => x.To!.Value.DayNumber - x.From!.Value.DayNumber < MaxPeriodInDays)
            .WithMessage($"O período não pode passar de {MaxPeriodInDays} dias.")
            .When(x => x.From is not null && x.To is not null);
    }
}

public sealed class GetCollectionPointDashboardHandler(
    ICollectionPointRepository pointRepository,
    IBeneficiaryRepository beneficiaryRepository,
    IDateTimeProvider clock)
    : IRequestHandler<GetCollectionPointDashboardQuery, ErrorOr<CollectionPointDashboardResponse>>
{
    private const int MaxDaysForDailySeries = 62;
    private const int RecentWithdrawalsShown = 5;

    public async Task<ErrorOr<CollectionPointDashboardResponse>> Handle(
        GetCollectionPointDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var pointId = CollectionPointId.From(query.PointId);
        var point = await pointRepository.GetByIdAsync(pointId, cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var (start, end) = ResolvePeriod(query);
        var withdrawals = await pointRepository.ListWithdrawalsBetweenAsync(
            pointId, ToUtcStartOfDay(start), ToUtcStartOfDay(end.AddDays(1)), cancellationToken);
        var registered = await beneficiaryRepository.ListByCollectionPointAsync(pointId, cancellationToken);
        var granularity = ChooseGranularity(start, end);

        return new CollectionPointDashboardResponse(
            query.PointId,
            start,
            end,
            withdrawals.Count,
            withdrawals.Select(w => w.BeneficiaryId).Distinct().Count(),
            await pointRepository.CountWithdrawalsAsync(pointId, cancellationToken),
            registered.Count,
            granularity,
            BuildSeries(withdrawals, start, end, granularity),
            await BuildRecentWithdrawalsAsync(withdrawals, cancellationToken));
    }

    private (DateOnly Start, DateOnly End) ResolvePeriod(GetCollectionPointDashboardQuery query)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return (query.From ?? monthStart, query.To ?? monthEnd);
    }

    private static DashboardGranularity ChooseGranularity(DateOnly start, DateOnly end) =>
        end.DayNumber - start.DayNumber < MaxDaysForDailySeries ? DashboardGranularity.Day : DashboardGranularity.Month;

    private static List<WithdrawalsSeriesPointResponse> BuildSeries(
        IReadOnlyList<Withdrawal> withdrawals,
        DateOnly start,
        DateOnly end,
        DashboardGranularity granularity)
    {
        var buckets = granularity == DashboardGranularity.Day
            ? EachDay(start, end)
            : EachMonthStart(start, end);
        var countByBucket = withdrawals
            .GroupBy(w => BucketOf(DateOnly.FromDateTime(w.OccurredAtUtc), granularity))
            .ToDictionary(g => g.Key, g => g.Count());

        return buckets
            .Select(bucket => new WithdrawalsSeriesPointResponse(bucket, countByBucket.GetValueOrDefault(bucket)))
            .ToList();
    }

    private static DateOnly BucketOf(DateOnly date, DashboardGranularity granularity) =>
        granularity == DashboardGranularity.Day ? date : new DateOnly(date.Year, date.Month, 1);

    private static IEnumerable<DateOnly> EachDay(DateOnly start, DateOnly end)
    {
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            yield return day;
        }
    }

    private static IEnumerable<DateOnly> EachMonthStart(DateOnly start, DateOnly end)
    {
        var lastMonth = new DateOnly(end.Year, end.Month, 1);
        for (var month = new DateOnly(start.Year, start.Month, 1); month <= lastMonth; month = month.AddMonths(1))
        {
            yield return month;
        }
    }

    private async Task<IReadOnlyList<RecentWithdrawalResponse>> BuildRecentWithdrawalsAsync(
        IReadOnlyList<Withdrawal> withdrawalsNewestFirst,
        CancellationToken cancellationToken)
    {
        var recent = withdrawalsNewestFirst.Take(RecentWithdrawalsShown).ToList();
        var beneficiaries = await beneficiaryRepository.ListByIdsAsync(
            recent.Select(w => w.BeneficiaryId).Distinct().ToList(), cancellationToken);
        var nameById = beneficiaries.ToDictionary(b => b.Id, b => b.Name);

        return recent
            .Select(w => new RecentWithdrawalResponse(
                w.Id.Value, nameById.GetValueOrDefault(w.BeneficiaryId, "Beneficiário removido"), w.OccurredAtUtc))
            .ToList();
    }

    private static DateTime ToUtcStartOfDay(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
