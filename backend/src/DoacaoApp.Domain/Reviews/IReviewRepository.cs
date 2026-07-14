using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Domain.Reviews;

/// <summary>Persistência do agregado <see cref="Review"/>.</summary>
public interface IReviewRepository
{
    void Add(Review review);

    Task<Review?> GetByIdAsync(ReviewId id, CancellationToken cancellationToken = default);

    /// <summary>Avaliações visíveis de um ponto, das mais recentes para as mais antigas.</summary>
    Task<IReadOnlyList<Review>> ListByPointAsync(
        CollectionPointId pointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumo (média + total) das avaliações dos pontos informados. Pontos sem avaliações
    /// simplesmente não aparecem no resultado.
    /// </summary>
    Task<IReadOnlyList<PointRatingSummary>> GetSummariesAsync(
        IReadOnlyCollection<CollectionPointId> pointIds,
        CancellationToken cancellationToken = default);
}
