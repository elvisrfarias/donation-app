using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Notices;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class NoticeRepository(AppDbContext context) : INoticeRepository
{
    public void Add(PointNotice notice) => context.Notices.Add(notice);

    public async Task<PointNotice?> GetByIdAsync(NoticeId id, CancellationToken cancellationToken = default) =>
        await context.Notices.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PointNotice>> ListPublicByPointAsync(
        CollectionPointId pointId,
        CancellationToken cancellationToken = default) =>
        await context.Notices
            .AsNoTracking()
            .Where(n => n.PointId == pointId && !n.IsArchived && n.Audience == NoticeAudience.Public)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PointNotice>> ListByPointAsync(
        CollectionPointId pointId,
        int take,
        CancellationToken cancellationToken = default) =>
        await context.Notices
            .AsNoTracking()
            .Where(n => n.PointId == pointId && !n.IsArchived)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PointNotice>> ListNetworkAsync(
        int take,
        CancellationToken cancellationToken = default) =>
        await context.Notices
            .AsNoTracking()
            .Where(n => !n.IsArchived)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}
