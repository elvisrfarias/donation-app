using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public void Add(Review review) => context.Reviews.Add(review);

    public async Task<Review?> GetByIdAsync(ReviewId id, CancellationToken cancellationToken = default) =>
        await context.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Review>> ListByPointAsync(
        CollectionPointId pointId,
        CancellationToken cancellationToken = default) =>
        await context.Reviews
            .AsNoTracking()
            .Where(r => r.PointId == pointId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PointRatingSummary>> GetSummariesAsync(
        IReadOnlyCollection<CollectionPointId> pointIds,
        CancellationToken cancellationToken = default)
    {
        if (pointIds.Count == 0)
        {
            return [];
        }

        var summaries = await context.Reviews
            .AsNoTracking()
            .Where(r => pointIds.Contains(r.PointId) && !r.IsHidden)
            .GroupBy(r => r.PointId)
            .Select(g => new PointRatingSummary(
                g.Key,
                g.Average(r => (double)r.Rating),
                g.Count()))
            .ToListAsync(cancellationToken);

        return summaries;
    }
}
