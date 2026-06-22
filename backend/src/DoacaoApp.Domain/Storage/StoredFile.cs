using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage.Events;
using ErrorOr;

namespace DoacaoApp.Domain.Storage;

/// <summary>
/// Metadado de um arquivo (tipicamente imagem) guardado num object storage compatível com S3
/// (MinIO ou Amazon S3). É a fonte da verdade da aplicação sobre o objeto: o bucket/chave onde
/// vive, a quem pertence, integridade e ciclo de vida. O binário em si fica no storage; aqui só
/// os metadados — o que mantém a tabela enxuta e pronta para migração entre provedores.
/// </summary>
public sealed class StoredFile : AggregateRoot<StoredFileId>
{
    private Dictionary<string, string> _metadata = new();

    // Usado pela materialização do EF Core.
    private StoredFile()
    {
        Bucket = string.Empty;
        ObjectKey = string.Empty;
        OriginalFileName = string.Empty;
        ContentType = string.Empty;
        Extension = string.Empty;
    }

    private StoredFile(
        StoredFileId id,
        StorageProvider provider,
        string bucket,
        string objectKey,
        string originalFileName,
        string extension,
        string contentType,
        long sizeBytes,
        FileVisibility visibility,
        DateTime createdAtUtc) : base(id)
    {
        Provider = provider;
        Bucket = bucket;
        ObjectKey = objectKey;
        OriginalFileName = originalFileName;
        Extension = extension;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Visibility = visibility;
        Status = StoredFileStatus.Available;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Provedor físico onde o objeto está hospedado.</summary>
    public StorageProvider Provider { get; private set; }

    /// <summary>Bucket que contém o objeto.</summary>
    public string Bucket { get; private set; }

    /// <summary>Chave (caminho) do objeto dentro do bucket. Único por bucket.</summary>
    public string ObjectKey { get; private set; }

    /// <summary>Nome original enviado pelo usuário (para download amigável).</summary>
    public string OriginalFileName { get; private set; }

    /// <summary>Extensão normalizada com ponto, ex.: ".png".</summary>
    public string Extension { get; private set; }

    /// <summary>MIME type (ex.: "image/png").</summary>
    public string ContentType { get; private set; }

    /// <summary>Tamanho em bytes.</summary>
    public long SizeBytes { get; private set; }

    /// <summary>Hash SHA-256 (hex) do conteúdo — integridade e deduplicação.</summary>
    public string? ChecksumSha256 { get; private set; }

    /// <summary>ETag retornado pelo storage no upload.</summary>
    public string? ETag { get; private set; }

    /// <summary>Largura em pixels (quando imagem).</summary>
    public int? Width { get; private set; }

    /// <summary>Altura em pixels (quando imagem).</summary>
    public int? Height { get; private set; }

    public StoredFileStatus Status { get; private set; }

    public FileVisibility Visibility { get; private set; }

    /// <summary>Tipo do dono (associação polimórfica solta), ex.: "CollectionPoint".</summary>
    public string? OwnerType { get; private set; }

    /// <summary>Id do dono (sem FK no banco — associação intencionalmente fraca).</summary>
    public Guid? OwnerId { get; private set; }

    /// <summary>Usuário que fez o upload (auditoria).</summary>
    public Guid? UploadedByUserId { get; private set; }

    /// <summary>Pares chave/valor extras (jsonb), para extensão sem mudança de schema.</summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public static ErrorOr<StoredFile> Create(
        StorageProvider provider,
        string? bucket,
        string? objectKey,
        string? originalFileName,
        string? contentType,
        long sizeBytes,
        FileVisibility visibility,
        DateTime createdAtUtc,
        Guid? uploadedByUserId = null,
        string? eTag = null,
        string? checksumSha256 = null,
        int? width = null,
        int? height = null,
        string? ownerType = null,
        Guid? ownerId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(objectKey))
        {
            return DomainErrors.StoredFile.KeyRequired;
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return DomainErrors.StoredFile.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return DomainErrors.StoredFile.ContentTypeRequired;
        }

        if (sizeBytes <= 0)
        {
            return DomainErrors.StoredFile.Empty;
        }

        var extension = (Path.GetExtension(originalFileName) ?? string.Empty).Trim().ToLowerInvariant();

        var file = new StoredFile(
            StoredFileId.New(),
            provider,
            bucket.Trim(),
            objectKey.Trim(),
            originalFileName.Trim(),
            extension,
            contentType.Trim().ToLowerInvariant(),
            sizeBytes,
            visibility,
            createdAtUtc)
        {
            ETag = eTag,
            ChecksumSha256 = checksumSha256,
            Width = width,
            Height = height,
            OwnerType = string.IsNullOrWhiteSpace(ownerType) ? null : ownerType.Trim(),
            OwnerId = ownerId,
            UploadedByUserId = uploadedByUserId,
        };

        if (metadata is { Count: > 0 })
        {
            file._metadata = new Dictionary<string, string>(metadata);
        }

        file.RaiseDomainEvent(new StoredFileUploaded(file.Id, file.ObjectKey, file.ContentType, file.SizeBytes));

        return file;
    }

    /// <summary>Vincula o arquivo a um dono (ex.: associa a foto a um ponto de coleta).</summary>
    public void AttachToOwner(string ownerType, Guid ownerId, DateTime updatedAtUtc)
    {
        OwnerType = string.IsNullOrWhiteSpace(ownerType) ? null : ownerType.Trim();
        OwnerId = ownerId;
        Touch(updatedAtUtc);
    }

    public void SetImageDimensions(int width, int height, DateTime updatedAtUtc)
    {
        Width = width;
        Height = height;
        Touch(updatedAtUtc);
    }

    public void SetChecksum(string checksumSha256, DateTime updatedAtUtc)
    {
        ChecksumSha256 = checksumSha256;
        Touch(updatedAtUtc);
    }

    public void ReplaceMetadata(IReadOnlyDictionary<string, string> metadata, DateTime updatedAtUtc)
    {
        _metadata = new Dictionary<string, string>(metadata);
        Touch(updatedAtUtc);
    }

    public void Quarantine(DateTime updatedAtUtc)
    {
        Status = StoredFileStatus.Quarantined;
        Touch(updatedAtUtc);
    }

    public void MarkAvailable(DateTime updatedAtUtc)
    {
        Status = StoredFileStatus.Available;
        Touch(updatedAtUtc);
    }

    /// <summary>Remoção lógica: o objeto é apagado do bucket, mas a linha permanece para auditoria.</summary>
    public void MarkDeleted(DateTime deletedAtUtc)
    {
        if (Status == StoredFileStatus.Deleted)
        {
            return;
        }

        Status = StoredFileStatus.Deleted;
        DeletedAtUtc = deletedAtUtc;
        Touch(deletedAtUtc);
        RaiseDomainEvent(new StoredFileDeleted(Id, ObjectKey));
    }

    private void Touch(DateTime whenUtc) => UpdatedAtUtc = whenUtc;
}
