namespace DoacaoApp.Domain.Storage;

/// <summary>Identificador fortemente tipado de um arquivo armazenado.</summary>
public readonly record struct StoredFileId(Guid Value)
{
    public static StoredFileId New() => new(Guid.NewGuid());

    public static StoredFileId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Backend físico onde o objeto está hospedado. Pensado para migração transparente.</summary>
public enum StorageProvider
{
    /// <summary>MinIO (compatível com a API do S3) — uso local/on-premise.</summary>
    MinIO = 1,

    /// <summary>Amazon S3.</summary>
    AmazonS3 = 2,
}

/// <summary>Ciclo de vida do arquivo dentro do sistema.</summary>
public enum StoredFileStatus
{
    /// <summary>Registro criado, upload ainda não confirmado.</summary>
    Pending = 1,

    /// <summary>Disponível para uso.</summary>
    Available = 2,

    /// <summary>Bloqueado (ex.: reprovado em verificação antivírus/moderação).</summary>
    Quarantined = 3,

    /// <summary>Removido logicamente (objeto apagado do bucket; metadados mantidos para auditoria).</summary>
    Deleted = 4,
}

/// <summary>Visibilidade pretendida do arquivo.</summary>
public enum FileVisibility
{
    /// <summary>Acesso só via URL pré-assinada e temporária.</summary>
    Private = 1,

    /// <summary>Pode ser exposto publicamente (ex.: foto de ponto de coleta).</summary>
    Public = 2,
}
