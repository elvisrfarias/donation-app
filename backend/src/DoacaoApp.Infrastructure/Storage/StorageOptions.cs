using DoacaoApp.Domain.Storage;

namespace DoacaoApp.Infrastructure.Storage;

/// <summary>
/// Configuração do object storage (seção "Storage"). Pensada para migração transparente:
/// para usar o Amazon S3 basta apontar <see cref="Provider"/>=AmazonS3, limpar
/// <see cref="ServiceUrl"/>/<see cref="PublicUrl"/> (usa o endpoint regional) e ajustar credenciais.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"MinIO" ou "AmazonS3".</summary>
    public string Provider { get; init; } = "MinIO";

    /// <summary>Endpoint que o servidor usa para falar com o storage (ex.: http://minio:9000).
    /// Vazio em produção AWS = usa o endpoint regional do S3.</summary>
    public string? ServiceUrl { get; init; }

    /// <summary>Endpoint embutido nas URLs pré-assinadas (o que o navegador acessa, ex.:
    /// http://localhost:9000). Vazio = usa o <see cref="ServiceUrl"/>/região. Resolve o clássico
    /// problema de host do MinIO em Docker sem quebrar a assinatura SigV4.</summary>
    public string? PublicUrl { get; init; }

    public string Region { get; init; } = "us-east-1";

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string Bucket { get; init; } = "doacao";

    /// <summary>MinIO exige path-style (true); o S3 aceita virtual-hosted (false).</summary>
    public bool ForcePathStyle { get; init; } = true;

    /// <summary>Validade (minutos) das URLs pré-assinadas de download.</summary>
    public int PresignExpiryMinutes { get; init; } = 60;

    public StorageProvider ResolveProvider() =>
        Enum.TryParse<StorageProvider>(Provider, ignoreCase: true, out var parsed)
            ? parsed
            : StorageProvider.MinIO;
}
