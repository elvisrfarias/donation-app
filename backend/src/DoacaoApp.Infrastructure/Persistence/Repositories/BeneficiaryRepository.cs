using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class BeneficiaryRepository(AppDbContext context) : IBeneficiaryRepository
{
    public void Add(Beneficiary beneficiary) => context.Beneficiaries.Add(beneficiary);

    public async Task<IReadOnlyList<Beneficiary>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await context.Beneficiaries
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Beneficiary>> ListByCollectionPointAsync(
        CollectionPointId pointId, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries
            .Where(b => b.RegisteredByPointId == pointId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Beneficiary>> ListByIdsAsync(
        IReadOnlyCollection<BeneficiaryId> ids, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries
            .AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .ToListAsync(cancellationToken);

    public async Task<Beneficiary?> GetByIdAsync(BeneficiaryId id, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Beneficiary?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries.FirstOrDefaultAsync(b => b.Cpf == cpf, cancellationToken);

    public async Task<bool> ExistsWithCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries.AnyAsync(b => b.Cpf == cpf, cancellationToken);

    public async Task<bool> ExistsWithCpfAsync(
        Cpf cpf, BeneficiaryId excludeBeneficiaryId, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries.AnyAsync(
            b => b.Cpf == cpf && b.Id != excludeBeneficiaryId, cancellationToken);
}
