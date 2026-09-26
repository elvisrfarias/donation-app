using DoacaoApp.Domain.Beneficiaries;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>Persistência do agregado <see cref="CollectionPoint"/>.</summary>
public interface ICollectionPointRepository
{
    void Add(CollectionPoint point);

    Task<CollectionPoint?> GetByIdAsync(CollectionPointId id, CancellationToken cancellationToken = default);

    /// <summary>Pontos ativos para a listagem pública (com endereço e contato).</summary>
    Task<IReadOnlyList<CollectionPoint>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Todos os pontos (ativos e inativos), para a listagem administrativa.</summary>
    Task<IReadOnlyList<CollectionPoint>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Total de beneficiários distintos já atendidos em cada ponto informado (indicador de impacto
    /// para a divulgação/ranking). Pontos sem atendimentos não aparecem no resultado.
    /// </summary>
    Task<IReadOnlyDictionary<CollectionPointId, int>> GetBeneficiariesServedAsync(
        IReadOnlyCollection<CollectionPointId> pointIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta as retiradas de um beneficiário dentro da janela [início, fim) considerando
    /// TODOS os pontos do município. É o coração do controle de equidade compartilhado.
    /// </summary>
    Task<int> CountBeneficiaryWithdrawalsAsync(
        BeneficiaryId beneficiaryId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Retiradas de um ponto dentro da janela [início, fim), da mais recente à mais antiga.</summary>
    Task<IReadOnlyList<Withdrawal>> ListWithdrawalsBetweenAsync(
        CollectionPointId pointId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Total de retiradas já registradas em um ponto.</summary>
    Task<int> CountWithdrawalsAsync(CollectionPointId pointId, CancellationToken cancellationToken = default);

    /// <summary>Histórico completo de retiradas de um beneficiário (todos os pontos), para relatórios.</summary>
    Task<IReadOnlyList<Withdrawal>> GetBeneficiaryWithdrawalHistoryAsync(
        BeneficiaryId beneficiaryId,
        CancellationToken cancellationToken = default);
}