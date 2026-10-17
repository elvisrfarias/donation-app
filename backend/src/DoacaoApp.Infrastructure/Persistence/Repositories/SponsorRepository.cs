using DoacaoApp.Domain.Sponsors;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// A categoria é gravada como texto, então a ordem Diamante → Ouro → Prata é feita em memória: são poucos
/// patrocinadores, e ordenar pelo texto daria Diamond → Gold → Silver só por coincidência alfabética.
/// </summary>
public sealed class SponsorRepository(AppDbContext context) : ISponsorRepository
{
    public void Add(Sponsor sponsor) => context.Sponsors.Add(sponsor);

    public void Remove(Sponsor sponsor) => context.Sponsors.Remove(sponsor);

    public async Task<Sponsor?> GetByIdAsync(SponsorId id, CancellationToken cancellationToken = default) =>
        await context.Sponsors.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sponsor>> ListAllAsync(CancellationToken cancellationToken = default) =>
        OrderedByTierThenName(await context.Sponsors.AsNoTracking().ToListAsync(cancellationToken));

    public async Task<IReadOnlyList<Sponsor>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        OrderedByTierThenName(await context.Sponsors.AsNoTracking().Where(s => s.IsActive).ToListAsync(cancellationToken));

    private static IReadOnlyList<Sponsor> OrderedByTierThenName(List<Sponsor> sponsors) =>
        [.. sponsors.OrderBy(s => s.Tier).ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)];
}
