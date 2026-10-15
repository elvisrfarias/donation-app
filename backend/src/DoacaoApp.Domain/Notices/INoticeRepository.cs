using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Domain.Notices;

/// <summary>Persistência do agregado <see cref="PointNotice"/>.</summary>
public interface INoticeRepository
{
    void Add(PointNotice notice);

    Task<PointNotice?> GetByIdAsync(NoticeId id, CancellationToken cancellationToken = default);

    /// <summary>Avisos públicos ativos de um ponto, dos mais recentes para os mais antigos.</summary>
    Task<IReadOnlyList<PointNotice>> ListPublicByPointAsync(
        CollectionPointId pointId,
        CancellationToken cancellationToken = default);

    /// <summary>Avisos ativos de um ponto (de qualquer público), dos mais recentes para os mais antigos.</summary>
    Task<IReadOnlyList<PointNotice>> ListByPointAsync(
        CollectionPointId pointId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Mural da rede: avisos ativos de todos os pontos, dos mais recentes para os mais antigos.</summary>
    Task<IReadOnlyList<PointNotice>> ListNetworkAsync(
        int take,
        CancellationToken cancellationToken = default);
}
