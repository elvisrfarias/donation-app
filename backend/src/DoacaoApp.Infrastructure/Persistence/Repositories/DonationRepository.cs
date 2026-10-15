using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Donations;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class DonationRepository(AppDbContext context) : IDonationRepository
{
    public void Add(Donation donation) => context.Donations.Add(donation);

    public async Task<Donation?> GetByIdAsync(DonationId id, CancellationToken cancellationToken = default) =>
        await context.Donations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Donation>> ListByPointAsync(
        CollectionPointId pointId,
        int take,
        CancellationToken cancellationToken = default) =>
        await context.Donations
            .AsNoTracking()
            .Where(d => d.PointId == pointId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}
