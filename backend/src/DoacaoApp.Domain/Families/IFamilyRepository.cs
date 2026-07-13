using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Domain.Families;

/// <summary>Persistência do agregado <see cref="Family"/> (inclui seus beneficiários).</summary>
public interface IFamilyRepository
{
    void Add(Family family);

    /// <summary>Lista todas as famílias com seus beneficiários.</summary>
    Task<IReadOnlyList<Family>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Carrega a família com seus beneficiários.</summary>
    Task<Family?> GetByIdAsync(FamilyId id, CancellationToken cancellationToken = default);

    /// <summary>Localiza a família dona de um beneficiário (carrega o agregado completo).</summary>
    Task<Family?> GetByBeneficiaryIdAsync(BeneficiaryId beneficiaryId, CancellationToken cancellationToken = default);

    /// <summary>Unicidade municipal de CPF entre beneficiários (todas as famílias).</summary>
    Task<bool> ExistsBeneficiaryWithCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);

    /// <summary>Unicidade municipal de CPF entre beneficiários, ignorando o próprio beneficiário (edição).</summary>
    Task<bool> ExistsBeneficiaryWithCpfAsync(
        Cpf cpf, BeneficiaryId excludeBeneficiaryId, CancellationToken cancellationToken = default);
}