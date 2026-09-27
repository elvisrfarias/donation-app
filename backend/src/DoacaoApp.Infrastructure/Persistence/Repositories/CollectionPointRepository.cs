using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Beneficiaries;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class CollectionPointRepository(AppDbContext context) : ICollectionPointRepository
{
    public void Add(CollectionPoint point) => context.CollectionPoints.Add(point);

    public async Task<CollectionPoint?> GetByIdAsync(CollectionPointId id, CancellationToken cancellationToken = default) =>
        await context.CollectionPoints
            .Include(p => p.Withdrawals)
            .Include(p => p.StockItems)
            .Include(p => p.BasketTemplates)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CollectionPoint>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await context.CollectionPoints
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CollectionPoint>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await context.CollectionPoints
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<CollectionPointId, int>> GetBeneficiariesServedAsync(
        IReadOnlyCollection<CollectionPointId> pointIds,
        CancellationToken cancellationToken = default)
    {
        if (pointIds.Count == 0)
        {
            return new Dictionary<CollectionPointId, int>();
        }

        var stats = await context.Withdrawals
            .AsNoTracking()
            .Where(w => pointIds.Contains(w.PointId))
            .GroupBy(w => w.PointId)
            .Select(g => new { PointId = g.Key, BeneficiariesServed = g.Select(w => w.BeneficiaryId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        return stats.ToDictionary(s => s.PointId, s => s.BeneficiariesServed);
    }

    public async Task<int> CountBeneficiaryBasketWithdrawalsAsync(
        BeneficiaryId beneficiaryId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default) =>
        await context.Withdrawals
            .CountAsync(
                w => w.BeneficiaryId == beneficiaryId
                     && w.BasketTemplateId != null
                     && w.OccurredAtUtc >= startInclusiveUtc
                     && w.OccurredAtUtc < endExclusiveUtc,
                cancellationToken);

    public async Task<IReadOnlyList<Withdrawal>> ListWithdrawalsBetweenAsync(
        CollectionPointId pointId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default) =>
        await context.Withdrawals
            .AsNoTracking()
            .Where(w => w.PointId == pointId
                        && w.OccurredAtUtc >= startInclusiveUtc
                        && w.OccurredAtUtc < endExclusiveUtc)
            .OrderByDescending(w => w.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<int> CountWithdrawalsAsync(
        CollectionPointId pointId,
        CancellationToken cancellationToken = default) =>
        await context.Withdrawals.CountAsync(w => w.PointId == pointId, cancellationToken);

    public async Task<IReadOnlyList<Withdrawal>> GetBeneficiaryWithdrawalHistoryAsync(
        BeneficiaryId beneficiaryId,
        CancellationToken cancellationToken = default) =>
        await context.Withdrawals
            .AsNoTracking()
            .Where(w => w.BeneficiaryId == beneficiaryId)
            .OrderByDescending(w => w.OccurredAtUtc)
            .ToListAsync(cancellationToken);
}