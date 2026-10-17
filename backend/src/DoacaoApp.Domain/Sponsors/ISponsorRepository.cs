namespace DoacaoApp.Domain.Sponsors;

/// <summary>Persistência do agregado <see cref="Sponsor"/>.</summary>
public interface ISponsorRepository
{
    void Add(Sponsor sponsor);

    void Remove(Sponsor sponsor);

    Task<Sponsor?> GetByIdAsync(SponsorId id, CancellationToken cancellationToken = default);

    /// <summary>Todos os patrocinadores (ativos e inativos), do maior patrocínio ao menor e por nome.</summary>
    Task<IReadOnlyList<Sponsor>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Patrocinadores ativos, do maior patrocínio ao menor e por nome — o que a página pública mostra.</summary>
    Task<IReadOnlyList<Sponsor>> ListActiveAsync(CancellationToken cancellationToken = default);
}
