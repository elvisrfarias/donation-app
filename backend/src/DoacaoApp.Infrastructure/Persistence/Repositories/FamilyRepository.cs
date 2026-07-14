using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class FamilyRepository(AppDbContext context) : IFamilyRepository
{
    public void Add(Family family) => context.Families.Add(family);

    public async Task<IReadOnlyList<Family>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await context.Families
            .Include(f => f.Beneficiaries)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    public async Task<Family?> GetByIdAsync(FamilyId id, CancellationToken cancellationToken = default) =>
        await context.Families
            .Include(f => f.Beneficiaries)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Family?> GetByBeneficiaryIdAsync(BeneficiaryId beneficiaryId, CancellationToken cancellationToken = default) =>
        await context.Families
            .Include(f => f.Beneficiaries)
            .FirstOrDefaultAsync(f => f.Beneficiaries.Any(b => b.Id == beneficiaryId), cancellationToken);

    public async Task<Family?> GetByBeneficiaryCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        await context.Families
            .Include(f => f.Beneficiaries)
            .FirstOrDefaultAsync(f => f.Beneficiaries.Any(b => b.Cpf == cpf), cancellationToken);

    public async Task<bool> ExistsBeneficiaryWithCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries.AnyAsync(b => b.Cpf == cpf, cancellationToken);

    public async Task<bool> ExistsBeneficiaryWithCpfAsync(
        Cpf cpf, BeneficiaryId excludeBeneficiaryId, CancellationToken cancellationToken = default) =>
        await context.Beneficiaries.AnyAsync(
            b => b.Cpf == cpf && b.Id != excludeBeneficiaryId, cancellationToken);
}