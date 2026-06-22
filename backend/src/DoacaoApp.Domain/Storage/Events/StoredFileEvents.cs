using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.Storage.Events;

/// <summary>Disparado quando um arquivo é enviado e registrado com sucesso.</summary>
public sealed record StoredFileUploaded(
    StoredFileId FileId,
    string ObjectKey,
    string ContentType,
    long SizeBytes) : IDomainEvent;

/// <summary>Disparado quando um arquivo é removido logicamente.</summary>
public sealed record StoredFileDeleted(
    StoredFileId FileId,
    string ObjectKey) : IDomainEvent;
