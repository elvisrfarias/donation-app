using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Domain.Beneficiaries;

/// <summary>Persistência do agregado <see cref="Beneficiary"/>.</summary>
public interface IBeneficiaryRepository
{
    void Add(Beneficiary beneficiary);

    /// <summary>Lista todos os beneficiários.</summary>
    Task<IReadOnlyList<Beneficiary>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista os beneficiários vinculados ao ponto informado.</summary>
    Task<IReadOnlyList<Beneficiary>> ListByCollectionPointAsync(
        CollectionPointId pointId, CancellationToken cancellationToken = default);

    /// <summary>Beneficiários com os ids informados (uso em listagens que precisam só do nome).</summary>
    Task<IReadOnlyList<Beneficiary>> ListByIdsAsync(
        IReadOnlyCollection<BeneficiaryId> ids, CancellationToken cancellationToken = default);

    Task<Beneficiary?> GetByIdAsync(BeneficiaryId id, CancellationToken cancellationToken = default);

    /// <summary>Localiza o beneficiário pelo CPF (busca rápida na retirada e nos vínculos).</summary>
    Task<Beneficiary?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);

    /// <summary>Unicidade municipal de CPF entre beneficiários.</summary>
    Task<bool> ExistsWithCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);

    /// <summary>Unicidade municipal de CPF entre beneficiários, ignorando o próprio beneficiário (edição).</summary>
    Task<bool> ExistsWithCpfAsync(
        Cpf cpf, BeneficiaryId excludeBeneficiaryId, CancellationToken cancellationToken = default);
}
