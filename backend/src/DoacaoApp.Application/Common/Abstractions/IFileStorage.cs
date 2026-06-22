using DoacaoApp.Domain.Storage;

namespace DoacaoApp.Application.Common.Abstractions;

/// <summary>Dados necessários para enviar um objeto ao storage.</summary>
public sealed record FileUploadRequest(
    string ObjectKey,
    Stream Content,
    string ContentType,
    long Length,
    FileVisibility Visibility,
    string OriginalFileName);

/// <summary>Resultado físico de um upload, devolvido pelo provedor.</summary>
public sealed record StoredObject(
    StorageProvider Provider,
    string Bucket,
    string ObjectKey,
    string ETag,
    long SizeBytes);

/// <summary>
/// Porta de armazenamento de objetos, agnóstica de provedor. A implementação fala a API do S3
/// (MinIO em dev, Amazon S3 em produção) — trocar de provedor é só mudar configuração, sem tocar
/// no domínio nem nos casos de uso. A política de expiração das URLs pré-assinadas vive na
/// implementação (configurável), por isso não é parâmetro aqui.
/// </summary>
public interface IFileStorage
{
    /// <summary>Envia o conteúdo para a chave informada e devolve os metadados físicos.</summary>
    Task<StoredObject> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gera uma URL temporária de download (GET) para a chave. Quando informado,
    /// <paramref name="downloadFileName"/> sugere o nome do arquivo ao navegador.
    /// </summary>
    Task<Uri> GetPresignedDownloadUrlAsync(
        string objectKey,
        string? downloadFileName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Remove o objeto do bucket. Idempotente: não falha se a chave não existir.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
