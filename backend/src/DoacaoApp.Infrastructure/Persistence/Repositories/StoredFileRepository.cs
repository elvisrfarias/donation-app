using DoacaoApp.Domain.Storage;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence.Repositories;

public sealed class StoredFileRepository(AppDbContext context) : IStoredFileRepository
{
    public void Add(StoredFile file) => context.StoredFiles.Add(file);

    public async Task<StoredFile?> GetByIdAsync(StoredFileId id, CancellationToken cancellationToken = default) =>
        await context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StoredFile>> ListByIdsAsync(
        IReadOnlyCollection<StoredFileId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await context.StoredFiles
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id) && f.Status != StoredFileStatus.Deleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> ListByOwnerAsync(
        string ownerType,
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        await context.StoredFiles
            .AsNoTracking()
            .Where(f => f.OwnerType == ownerType
                        && f.OwnerId == ownerId
                        && f.Status != StoredFileStatus.Deleted)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<StoredFile?> FindByChecksumAsync(
        string checksumSha256,
        CancellationToken cancellationToken = default) =>
        await context.StoredFiles
            .AsNoTracking()
            .Where(f => f.ChecksumSha256 == checksumSha256 && f.Status == StoredFileStatus.Available)
            .FirstOrDefaultAsync(cancellationToken);
}
