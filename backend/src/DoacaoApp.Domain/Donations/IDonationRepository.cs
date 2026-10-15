using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Domain.Donations;

/// <summary>Persistência do agregado <see cref="Donation"/>.</summary>
public interface IDonationRepository
{
    void Add(Donation donation);

    Task<Donation?> GetByIdAsync(DonationId id, CancellationToken cancellationToken = default);

    /// <summary>Doações do ponto, das mais recentes para as mais antigas.</summary>
    Task<IReadOnlyList<Donation>> ListByPointAsync(
        CollectionPointId pointId,
        int take,
        CancellationToken cancellationToken = default);
}
