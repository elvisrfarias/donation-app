using DoacaoApp.Domain.Families;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>Persistência do agregado <see cref="CollectionPoint"/>.</summary>
public interface ICollectionPointRepository
{
    void Add(CollectionPoint point);

    Task<CollectionPoint?> GetByIdAsync(CollectionPointId id, CancellationToken cancellationToken = default);

    /// <summary>Pontos ativos para a listagem pública (com endereço e contato).</summary>
    Task<IReadOnlyList<CollectionPoint>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta as retiradas de uma família dentro da janela [início, fim) considerando
    /// TODOS os pontos do município. É o coração do controle de equidade compartilhado.
    /// </summary>
    Task<int> CountFamilyWithdrawalsAsync(
        FamilyId familyId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Histórico completo de retiradas de uma família (todos os pontos), para relatórios.</summary>
    Task<IReadOnlyList<Withdrawal>> GetFamilyWithdrawalHistoryAsync(
        FamilyId familyId,
        CancellationToken cancellationToken = default);
}