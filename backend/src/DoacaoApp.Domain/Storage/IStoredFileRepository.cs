namespace DoacaoApp.Domain.Storage;

/// <summary>Persistência do agregado <see cref="StoredFile"/>.</summary>
public interface IStoredFileRepository
{
    void Add(StoredFile file);

    Task<StoredFile?> GetByIdAsync(StoredFileId id, CancellationToken cancellationToken = default);

    /// <summary>Arquivos (não removidos) de um dono — ex.: todas as fotos de um ponto de coleta.</summary>
    Task<IReadOnlyList<StoredFile>> ListByOwnerAsync(
        string ownerType,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>Busca por hash para deduplicação (retorna o primeiro disponível com o mesmo conteúdo).</summary>
    Task<StoredFile?> FindByChecksumAsync(string checksumSha256, CancellationToken cancellationToken = default);
}
