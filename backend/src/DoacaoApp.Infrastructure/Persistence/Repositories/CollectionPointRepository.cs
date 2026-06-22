using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class CollectionPointRepository(AppDbContext context) : ICollectionPointRepository
{
    public void Add(CollectionPoint point) => context.CollectionPoints.Add(point);

    public async Task<CollectionPoint?> GetByIdAsync(CollectionPointId id, CancellationToken cancellationToken = default) =>
        await context.CollectionPoints.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CollectionPoint>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await context.CollectionPoints
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<int> CountFamilyWithdrawalsAsync(
        FamilyId familyId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default) =>
        await context.Withdrawals
            .CountAsync(
                w => w.FamilyId == familyId
                     && w.OccurredAtUtc >= startInclusiveUtc
                     && w.OccurredAtUtc < endExclusiveUtc,
                cancellationToken);

    public async Task<IReadOnlyList<Withdrawal>> GetFamilyWithdrawalHistoryAsync(
        FamilyId familyId,
        CancellationToken cancellationToken = default) =>
        await context.Withdrawals
            .AsNoTracking()
            .Where(w => w.FamilyId == familyId)
            .OrderByDescending(w => w.OccurredAtUtc)
            .ToListAsync(cancellationToken);
}